using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace MapMagic
{
	[System.Serializable] //Calculate and Apply could not be serialized
	public class ThreadWorker 
	{
		public static Thread mainThread = null;
		public static bool isPlaymode = false;

		public static List<ThreadWorker> queue = new List<ThreadWorker>(); //have to remove from queue on restart, so it's actually List
		
		public static IEnumerator CurrentCoroutine;
		public static ThreadWorker currentCoroutineWorker;

		public static bool on = true;
		public static bool profile = false;
		public static bool multithreading = true;
		public static bool oneThreadPerFrame = true; //for non-multithreaded mode
		public static int maxThreads = 3; 
		public static bool autoMaxThreads = true;
		public static int maxApplyTime = 15;
		public static int sleep = 0;


		#region Log
			public static bool logging = true;
			public static List<ThreadWorker> recent = new List<ThreadWorker>();
			public struct LogEntity
			{
				public ThreadWorker worker;
				public string name;
				public Stage stage;
				public bool stop;
				public int threadId;
				public DateTime time;
				public bool locked;

				public enum ConditionMet { Null, True, False }
				public ConditionMet threadCondition;
				public ConditionMet applyCondition;

				public string Print () 
				{ 
					string prefix = worker.name + " " + name;
					for (int numSymbols=prefix.Length; numSymbols<50; numSymbols+=10)
						prefix += "\t";
				
					return prefix + 
						"\t (stage:" + stage + (stage.ToString().Length<10? "\t" : "") +
						"\t threadId:" + threadId + 
						"\t stop:" + stop + 
						"\t thC:" + threadCondition + " apC:" + applyCondition + 
						"\t time:" + time.Minute +":" + time.Second + ":" + time.Millisecond + ")" +
						"\t lock:" + locked + "\n"; 

				}
			}
			public static List<LogEntity> log = new List<LogEntity>();

			[System.Diagnostics.Conditional("WDEBUG")]
			public static void Log (ThreadWorker worker, string message, bool logToConsole=false)
			{
				#if WDEBUG
				//seems that conditional does not work in some cases

				if (!logging) return;

				if (log.Count > 50000) log.RemoveRange(0,20000);

				LogEntity logEntity = new LogEntity() { worker=worker, name=message, stage=worker.stage, stop=worker.stop, threadId=Thread.CurrentThread.ManagedThreadId };
				if (worker.threadCondition != null) 
				{
					if (worker.threadCondition()) logEntity.threadCondition = LogEntity.ConditionMet.True;
					else logEntity.threadCondition = LogEntity.ConditionMet.False;
				}
				if (worker.applyCondition != null) 
				{
					if (worker.applyCondition()) logEntity.applyCondition = LogEntity.ConditionMet.True;
					else logEntity.applyCondition = LogEntity.ConditionMet.False;
				}
				logEntity.time = DateTime.Now;

				logEntity.locked = worker.lockWasTaken;

				log.Add(logEntity);

				if (logToConsole) Debug.LogError(worker.name + " " + message);

				#endif
			}
			public static string PrintLog () { List<ThreadWorker> tmp = null; return PrintLog(tmp); }
			public static string PrintLog (ThreadWorker worker) { List <ThreadWorker> workers = new List<ThreadWorker>(); workers.Add(worker); return PrintLog(workers); }
			public static string PrintLog (List<ThreadWorker> workers)
			{
				if (log==null) return "No Logging Enabled";
				string result = "";
				for (int i=0; i<log.Count; i++)
				{
					if (workers!=null && workers.Count!=0 && !workers.Contains(log[i].worker)) continue;
					result += i + ". " + log[i].Print();
				}
				return result;
			}
			public static List<ThreadWorker> currentlySelectedWorkers = null;

			public int startNum = 0;
			public int stopNum = 0;
		#endregion


		public static void Refresh ()
		{
			#if WDEBUG
			Profiler.BeginSample("Refresh Threads");
			#endif

			//checking main thread for IsMainThread function
			if (mainThread==null) mainThread = Thread.CurrentThread;
  
          	//setting the number of max threads if auto threads is on
          	if (autoMaxThreads) maxThreads = SystemInfo.processorCount - 1;

			//checking playmode
			#if UNITY_EDITOR
			isPlaymode = UnityEditor.EditorApplication.isPlaying;
			#else
			isPlaymode = true;
			#endif

			if (!on) return;

			//updating
			UpdateThreads();
 
			UpdateApply();

			//clearing queue if it has all of the workers idle
			bool allIdle = true;
			int queueCount = queue.Count;
			for (int i=0; i<queueCount; i++)
				if (queue[i].stage != Stage.ready && queue[i].stage != Stage.blank) { allIdle=false; break; }
			if (allIdle && queueCount!=0) 
			{
				if (logging) { recent.Clear(); recent.AddRange(queue); }
				queue.Clear();
			} 

			#if WDEBUG
			Profiler.EndSample();
			#endif
		}


		public static void UpdateApply ()
		{
			if (!on || queue.Count==0) return;

			#if WDEBUG
			Profiler.BeginSample("Update Apply");
			#endif
		
			System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
			timer.Start();

			while (timer.ElapsedMilliseconds < maxApplyTime)
			{
				//if couroutine has started - moving coroutine
				if (CurrentCoroutine != null) { currentCoroutineWorker.CoroutineFn(); continue; }

				//finding suitable worker with highest priority
				float maxProirity = -2000000;
//				int maxProirityNum = -1;
				ThreadWorker maxPriorityWorker = null;

				int queueCount = queue.Count;
				for (int i=0; i<queueCount; i++)
				{
					ThreadWorker worker = queue[i];

					if (worker==null) continue; //if object destroyed
					if (worker.priority < maxProirity) continue;
					if (worker.stage!=Stage.applyEnqueued && worker.stage!=Stage.prepareEnqueued && worker.stage!=Stage.coroutineEnqueued && worker.stage!=Stage.coroutineRunning) continue; //other stage
					if (worker.stage==Stage.prepareEnqueued && worker.prepareCondition!=null && !worker.prepareCondition()) continue;
					if (worker.stage==Stage.applyEnqueued && worker.applyCondition!=null && !worker.applyCondition()) continue; //if apply condition has not met
					if (worker.stage==Stage.coroutineEnqueued && worker.coroutineCondition!=null && !worker.coroutineCondition()) continue; //if coroutine condition has not met (note that conditions is checked only before starting coroutine)
					
					maxPriorityWorker = worker;
					maxProirity = worker.priority; 
//					maxProirityNum = i;
				}

				//no suitable applies
				if (maxPriorityWorker==null) break;

				//apply
				//lock (maxPriorityWorker.locker)
				Monitor.Enter(maxPriorityWorker.locker); maxPriorityWorker.lockWasTaken=true;
				try
				{
					if (logging) Log(maxPriorityWorker, "Refresh:ApplyPrepSelected");

					if (maxPriorityWorker.stage == Stage.prepareEnqueued)
					{
						//maxPriorityWorker.SwitchStage(Stage.applyRunning);
						maxPriorityWorker.PrepareFn();
					}

					if (maxPriorityWorker.stage == Stage.applyEnqueued) 
					{
						//maxPriorityWorker.SwitchStage(Stage.applyRunning);
						maxPriorityWorker.ApplyFn();
					}

					if (maxPriorityWorker.stage == Stage.coroutineEnqueued || maxPriorityWorker.stage == Stage.coroutineRunning) 
					{
						maxPriorityWorker.CoroutineFn();
					}
				}
				finally { Monitor.Exit(maxPriorityWorker.locker); maxPriorityWorker.lockWasTaken=false; }
			}

			#if WDEBUG
			Profiler.EndSample();
			#endif
		}

		public static void UpdateThreads () //called each time any thread is complete
		{try{
			if (!on || queue.Count==0) return;

			#if WDEBUG
			if (IsMainThread) Profiler.BeginSample("Update Threads");
			#endif
			
			int threadsRunning = 0; //current number of threads then multithreading is on

			//calculating number of threads running
			int queueCount = queue.Count;
			for (int i=0; i<queueCount; i++)
				if (queue[i].stage == Stage.threadRunning) threadsRunning++;

			//guard if all possible threads already started: exit with no queue locking
			if (threadsRunning>=maxThreads) 
			{
				#if WDEBUG
				if (IsMainThread) Profiler.EndSample();
				#endif

				return;
			}

			//staring new threads
			lock (queue)
			while (threadsRunning<maxThreads)
			{
				//finding suitable worker with highest priority
				float maxProirity = -2000000;
//				int maxProirityNum = -1;
				ThreadWorker maxPriorityWorker = null;

				queueCount = queue.Count;
				for (int i=0; i<queueCount; i++)
				{
					ThreadWorker worker = queue[i];
					if (worker==null) continue;
					if (worker.priority < maxProirity) continue;
					if (worker.stage!=Stage.threadEnqueued) continue; //if object destroyed or other stage
					if (worker.threadCondition!=null && !worker.threadCondition()) continue;

					maxPriorityWorker = worker; 
					maxProirity = worker.priority; 
//					maxProirityNum = i;
				}

				//no suitable threads
				if (maxPriorityWorker==null) break; 

				//starting thread
				//lock (maxPriorityWorker.locker)
				Monitor.Enter(maxPriorityWorker.locker); maxPriorityWorker.lockWasTaken=true;
				try
				{
					if (maxPriorityWorker.stage != Stage.threadEnqueued) return; //this could happen if two threads selecting one worker, or worker stopped while being selected 
					
					if (logging) Log(maxPriorityWorker, "Refresh:ThreadSelected (max" + maxProirity + ") ");  

					threadsRunning++;

					if (multithreading)
					{
						maxPriorityWorker.thread = new Thread(maxPriorityWorker.ThreadFn);

						maxPriorityWorker.thread.IsBackground = true;
						maxPriorityWorker.SwitchStage(Stage.threadRunning, "Refresh: start thread"); //before actually starting

						maxPriorityWorker.thread.Start();
					}
					else { maxPriorityWorker.ThreadFn(); if (IsMainThread) UnityEngine.Profiling.Profiler.EndSample(); if (oneThreadPerFrame) break; }
				}
				finally { Monitor.Exit(maxPriorityWorker.locker); maxPriorityWorker.lockWasTaken=false; }
			}

			#if WDEBUG
			if (IsMainThread) Profiler.EndSample();
			#endif

		} catch (System.Exception e) {Debug.LogError("Spinner Error: " + e); }
		}



		public string name;

		public delegate void ActionDelegate();
		public delegate IEnumerator CoroutineDelegate();
		public event ActionDelegate Prepare;
		public event ActionDelegate Calculate;
		public event ActionDelegate Apply;
		public CoroutineDelegate Coroutine;

		public Thread thread; //public for debug purpose
		public float priority = 1;
		private readonly object locker = new object();
		private bool lockWasTaken = false;
		private readonly object threadLocker = new object(); //used to lock generate function only, not worker (using single locker for this will make thread wait until generate end to perform stop)

		public enum Stage { blank, prepareEnqueued, threadEnqueued, threadRunning, applyEnqueued, coroutineEnqueued, coroutineRunning, ready, stop, restart };
		public Stage stage = Stage.blank; //public for test purpose

		public bool StopCallback (float progress) { return stage == Stage.stop || stage == Stage.restart; }
		public bool stop {get{ return stage == Stage.stop || stage == Stage.restart; }}
		public bool processing {get{ return stage != Stage.blank && stage != Stage.ready; }}
		public bool coroutine {get { return stage != Stage.coroutineRunning; }}
		public bool calculated {get{ return stage == Stage.applyEnqueued || stage==Stage.coroutineEnqueued || stage==Stage.coroutineRunning || stage == Stage.ready; }}
		public bool idle {get { return stage == Stage.ready || stage == Stage.blank; }}
		public bool blank {get { return stage == Stage.blank; }}
		public bool initialized {get { return Prepare!=null || Calculate!=null || Apply!=null; }}
		public bool ready  //resets on Stop, Start or any other stage change
		{
			get{ return stage == Stage.ready; } 
			set{ 
				Log (this, "Changing ready val: " + value);
				if (value==true)
				{
					switch (stage)
					{
						case Stage.ready: break; //do nothing
						case Stage.blank: SwitchStage(Stage.ready, "Changing ready b->r"); break;
						default: Log(this, "Enabling ready val in non ready/blank stage: " + stage, logToConsole: true); break;
					}
				}
				else //disabling
				{
					switch (stage)
					{
						case Stage.blank: case Stage.stop: break; //do nothing
						case Stage.ready: SwitchStage(Stage.blank, "Changing ready r->b"); break;
						default: Log(this, "Disabling ready val in non ready/blank stage: " + stage, logToConsole: true); break;
					}
				}
			}
		} 

		public Func<bool> prepareCondition;
		public Func<bool> threadCondition; //will not start thread unless condition is met
		public Func<bool> applyCondition; //will not start apply unless condition is met
		public Func<bool> coroutineCondition;

		public string tag;
		public float progress;
		public static Dictionary<string,float> completedTags = new Dictionary<string, float>();
		public static Dictionary<string,float> totalTags = new Dictionary<string, float>();

		#if WDEBUG
		public System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
		#endif
		
		public ThreadWorker () { }
		public ThreadWorker (string name, string tag=null) { this.name = name; this.tag = tag; }

		public void SwitchStage (Stage newStage, string message)
		{
			Monitor.Enter(locker);

			try 
			{
				if (newStage == Stage.prepareEnqueued && Prepare == null) newStage = Stage.threadEnqueued;
				if (newStage == Stage.threadEnqueued && Calculate == null) newStage = Stage.applyEnqueued;
				if (newStage == Stage.applyEnqueued && Apply == null) newStage = Stage.coroutineEnqueued;
				if (newStage == Stage.coroutineEnqueued && Coroutine == null) newStage = Stage.ready;
				//idle, stop and restart are not mentioned - they don't have to be skipped

				string postfix = logging? " (" + stage + "->" + newStage + ")" : null;

				#if WDEBUG
				if (timer != null && (newStage==Stage.blank || newStage==Stage.ready)) timer.Stop();
				#endif

				stage = newStage;

				if (logging) Log(this, message + postfix);
			}
			finally { Monitor.Exit(locker); }
		}

		public void Stop ()
		{
			Log(this, "StopCommand");

			Monitor.Enter(locker); lockWasTaken=true;
			try 
			{
				switch (stage)
				{
					case Stage.blank: Log(this,"Stop: Already stopped"); break;
					case Stage.ready: 
						SwitchStage(Stage.blank, "Stop: Resetting ready mark"); 
						break;
					case Stage.prepareEnqueued: case Stage.threadEnqueued: case Stage.applyEnqueued: case Stage.coroutineEnqueued:
						SwitchStage(Stage.blank, "Stop: Cancel enqueue");
						break;
					case Stage.threadRunning:
						SwitchStage(Stage.stop, "Stop: Entering stop mode");
						break;
					case Stage.coroutineRunning:
						CurrentCoroutine=null;
						SwitchStage(Stage.blank, "Stop: Cancel coroutine");
						break;
					case Stage.restart:
						SwitchStage(Stage.stop, "Stop: Cancel restart mode");
						break;
					case Stage.stop:
						Log(this, "Stop: Already in stop mode");
						break;
					default: Log(this,"Stop: Unexpected Stage: " + stage, logToConsole:true); break;
				}
			}
			finally { Monitor.Exit(locker); lockWasTaken=false; }

			if (logging) stopNum++;
		}

		public void Start ()
		{
			//Before debugging why generate does not start at all make sure Threader.Refresh() is called in Update
			
			Log(this, "StartCommand");

			//non-idle stage - this should not happen
			if (stage!=Stage.blank && stage!=Stage.ready && !queue.Contains(this)) 
			{ 
				Log(this,"Enqueue Non-idle stage", logToConsole:true); 
				queue.Add(this); 
				SwitchStage(Stage.blank, "Resetting Non-idle stage"); 
			}

			#if WDEBUG
			if (timer != null) { timer.Reset(); timer.Start(); }
			#endif

			Monitor.Enter(locker); lockWasTaken=true;  //worker should not be selected by Thread Update
			try 
			{
				switch (stage)
				{
					case Stage.blank: case Stage.ready:
						if (!queue.Contains(this)) { Log(this,"Enqueue"); queue.Add(this); }
						SwitchStage(Stage.prepareEnqueued, "Start: Running again (idle)");
						break;
					case Stage.prepareEnqueued: case Stage.threadEnqueued: case Stage.applyEnqueued: case Stage.coroutineEnqueued: //running again if no action is performed
						SwitchStage(Stage.prepareEnqueued, "Start: Running again (no action)");
						break;
					case Stage.threadRunning: //restarting if processing or stopping
					case Stage.stop:
						SwitchStage(Stage.restart, "Start: Restart thread mode");
						break;
					case Stage.coroutineRunning: //quit coroutine and restarting manually
						CurrentCoroutine=null;
						SwitchStage(Stage.prepareEnqueued,"Start: Restart coroutine mode");
						break;
					case Stage.restart: Log(this,"Start: Already in restart mode\t"); break; //do nothing if it was restarted, it will be returned to prepare on stage exit
					default: Log(this,"Start: Unexpected Stage: " + stage, logToConsole:true); break;
				}
			}
			finally { Monitor.Exit(locker); lockWasTaken=false; }

			if (logging) startNum++;
		}


		private void PrepareFn ()
		{
			if (logging) Log(this, "PrepareCalled");

			try 
			{ 
				if (Prepare != null) Prepare(); 

				//lock (locker) //already locked in thread/apply update
				switch (stage)
				{
					case Stage.stop: SwitchStage(Stage.blank, "PrepareExit_WithStop"); break;
					case Stage.restart: SwitchStage(Stage.prepareEnqueued, "PrepareExit_ReassigningEnqueue"); break;
					case Stage.prepareEnqueued: SwitchStage(Stage.threadEnqueued, "PrepareExit_OK"); break;
					default: Log(this,"PrepareExit_UnexpectedStage: " + stage, logToConsole:true); break;
				}
			}
			catch (System.Exception e) 
			{ 
				Debug.LogError("Prepare Error: " + e); 
				SwitchStage(Stage.blank, "Prepare Error"); 
			}

			if (logging) Log(this, "PrepareEnds");
		}


		private void ThreadFn ()
		{
			try
			{
				//generating
				if (Calculate != null) 
					lock (threadLocker)
						Calculate();
				
				//debug sleep
				if (sleep >= 1) Thread.Sleep(sleep);

				//pausing thread if workers turned off
				//while (!ThreadWorker.on) Thread.Sleep(500);

				Monitor.Enter(locker); lockWasTaken=true;   //should not be modified with update, start and stop now
				try  
				{
					switch (stage)
					{
						case Stage.stop: SwitchStage(Stage.blank,"ThreadExit_WithStop"); break;
						case Stage.restart: SwitchStage(Stage.prepareEnqueued, "ThreadExit_ReassigningEnqueue"); break;
						case Stage.threadEnqueued: case Stage.threadRunning: SwitchStage(Stage.applyEnqueued, "ThreadExit_OK"); if (this==null) Debug.Log("Thread: ADDING NULL TO APPLY"); break;
						default: Log(this,"ThreadExit_UnexpectedStage: " + stage, logToConsole:true); break;
					}
				}
				finally { Monitor.Exit(locker); lockWasTaken=false; }
			}

			catch (System.Exception e) 
			{ 
				Debug.LogError("Thread Error: " + e); 
				SwitchStage(Stage.blank, "Thread Error"); 
			}

			finally
			{
				try  { if (multithreading) UpdateThreads(); } //starting new thread in queue
				catch (System.Exception e) { Debug.LogError("Starting new thread error: " + e); }
			}
		}


		private void ApplyFn ()
		{
			try 
			{ 
				//apply
				if (Apply != null) Apply(); 

				//stopping/restarting
				//lock (locker) //already locked in thread/apply update
				switch (stage)
				{
					case Stage.stop: SwitchStage(Stage.blank, "ApplyExit_WithStop"); break;
					case Stage.restart: SwitchStage(Stage.prepareEnqueued, "ApplyExit_ReassigningEnqueue"); break;
					case Stage.applyEnqueued: SwitchStage(Stage.coroutineEnqueued, "ApplyExit_OK"); break;
					default: Log(this,"ApplyExit_UnexpectedStage: " + stage, logToConsole:true); break;
				}
			}
			catch (System.Exception e) 
			{ 
				Debug.LogError("Apply Error: " + e); 
				SwitchStage(Stage.blank, "Apply Error");
			}
		}

		private void CoroutineFn ()
		{
		//	try
		//	{
				if (stage!=Stage.coroutineRunning) 
				{
					SwitchStage(Stage.coroutineRunning, "Switch stage in coroutine");
					CurrentCoroutine = Coroutine();
					currentCoroutineWorker = this;
				}

				bool eoc = !CurrentCoroutine.MoveNext();

			//	if (stage==Stage.stop) { CurrentCoroutine=null; SwitchStage(Stage.blank, "CoroutineExit_WithStop"); }
			//	else if (stage==Stage.restart) { CurrentCoroutine=null; SwitchStage(Stage.prepareEnqueued, "CoroutineExit_ReassigningEnqueue"); }
				if (eoc) { CurrentCoroutine=null; currentCoroutineWorker=null; SwitchStage(Stage.ready, "CoroutineExit_OK"); }
		//	}
		//	catch (System.Exception e) 
		//	{ 
		//		Debug.LogError("Coroutine Error: " + e); 
		//		CurrentCoroutine=null;
		//		currentCoroutineWorker = null;
		//		SwitchStage(Stage.blank, "Coroutine Error");
		//	} 
		}


		public void FinalizeNow ()
		{
			Monitor.Enter(locker); lockWasTaken=true;
			try 
			{
				if (stage == Stage.prepareEnqueued) PrepareFn();
				if (stage == Stage.threadEnqueued) ThreadFn();
				if (stage == Stage.applyEnqueued) ApplyFn();
				//TODO: coroutine
			}
			finally { Monitor.Exit(locker); lockWasTaken=false; }
		}

		public void ForceAll () { Start(); FinalizeNow(); }


		public static bool IsMainThread
		{get{
			return mainThread==null || mainThread.Equals(Thread.CurrentThread);
		}}



		/*public static float GetProgress (string tag) {float tmp1=0; float tmp2=0; float tmp3=0; return GetProgress(tag, out tmp3, out tmp1, out tmp2); }

		public static float GetProgress (string tag, out float calculatedSum, out float completeSum, out float totalSum)
		{
			calculatedSum = 0;
			completeSum = 0;
			totalSum = 0;

			int queueCount = queue.Count;
			for (int i=0; i<queueCount; i++)
			{
				ThreadWorker worker = queue[i];
				if (worker.tag != tag) continue;

				totalSum += 1;
				switch (worker.stage)
				{
					case Stage.applyEnqueued: case Stage.coroutineEnqueued: case Stage.coroutineRunning: calculatedSum += 1; break;
					//case Stage.threadRunning: calculatedSum += worker.progress; break;
					//case Stage.blank:  calculatedSum += 1; completeSum += 1; break;
					case Stage.ready: calculatedSum += 1; completeSum += 1; break;
				}
			}

			return completeSum / totalSum;
		}*/

		public static bool IsWorking (string tag)
		{
			//if (queue.Count == 0) return false;	//will return false in the end anyways

			int queueCount = queue.Count;
			for (int i=0; i<queueCount; i++)
			{
				ThreadWorker worker = queue[i];
				if (!worker.tag.Contains(tag)) continue;
				if (worker.stage!=Stage.blank && worker.stage!=Stage.ready) return true; 
			}

			return false;
		}

		public static void GetProgresByTag (string tag, out bool contained, out bool calculated, out bool ready)
		{
			if (profile) UnityEngine.Profiling.Profiler.BeginSample("Get Progress Tag");

			int queueCount = queue.Count;

			contained = false; calculated = true; ready = true;

			for (int i=0; i<queueCount; i++)
			{
				ThreadWorker worker = queue[i];
				//if (!worker.tag.Contains(tag)) continue;
				if (worker.tag != tag) continue;

				contained = true;

				if (worker.stage!=Stage.applyEnqueued && worker.stage!=Stage.coroutineEnqueued && worker.stage!=Stage.coroutineRunning && worker.stage!=Stage.ready)
					{ calculated = false; ready = false; break; }
				
				if (worker.stage!=Stage.ready) ready = false; //to break to know if it is calculated
			}

			if (!contained) { calculated=false; ready=false; }

			if (profile) UnityEngine.Profiling.Profiler.EndSample();
		}

		public static void GetProgresByTag (string tag, out float contained, out float calculated, out float ready)
		{
			if (profile) UnityEngine.Profiling.Profiler.BeginSample("Get Progress Tag");

			DictTuple<string, bool,bool> dict = new DictTuple<string, bool,bool>();

			int queueCount = queue.Count;

			contained = 0; calculated = 0; ready = 0;

			for (int i=0; i<queueCount; i++)
			{
				ThreadWorker worker = queue[i];
				if (worker.stage==Stage.stop || worker.stage==Stage.blank) continue;
				if (!worker.tag.Contains(tag)) continue;

				//contains
				if (!dict.ContainsKey(worker.tag)) dict.Add(worker.tag, true,true);

				//calculated
				TupleSet<bool,bool> tuple = dict[worker.tag];
				if (worker.stage!=Stage.applyEnqueued && worker.stage!=Stage.coroutineEnqueued && worker.stage!=Stage.coroutineRunning && worker.stage!=Stage.ready)
					{ tuple.item1=false; tuple.item2=false; dict[worker.tag] = tuple; }

				//ready
				if (worker.stage!=Stage.ready) { tuple.item2=false; dict[worker.tag] = tuple; }
			}

			//calculating total statistics
			contained = dict.Count;
			foreach (TupleSet<bool,bool> tuple in dict.Values())
			{
				if (tuple.item1) calculated++;
				if (tuple.item2) ready++;
			}

			if (profile) UnityEngine.Profiling.Profiler.EndSample();
		}

		public void OnGUI (Layout layout)
		{
			layout.Par();
			layout.Label(name, layout.Inset(0.2f), fontStyle:FontStyle.Bold); layout.Inset(0.01f);
			layout.Field(stage, "Stg:", layout.Inset(0.2f), fieldSize:0.73f); layout.Inset(0.01f);
			layout.Field(stop, "Stp:", layout.Inset(0.1f)); layout.Inset(0.01f);
			layout.Label("Prt:"+priority, layout.Inset(0.15f)); layout.Inset(0.01f);

			layout.Label("tc:" + (threadCondition==null? "-" : (threadCondition()? "V" : "X")), layout.Inset(0.06f));
			layout.Label("ac:" + (applyCondition==null? "-" : (applyCondition()? "V" : "X")), layout.Inset(0.06f));
			layout.Label("rd:" + (ready? "V" : "X"), layout.Inset(0.06f));
			layout.Inset(0.01f);

			if (layout.Button("Log", layout.Inset(0.05f))) 
			{
				currentlySelectedWorkers = new List<ThreadWorker>();
				currentlySelectedWorkers.Add(this);
				Debug.Log(name + " Log:\n\n" + PrintLog(currentlySelectedWorkers));
			}
			if (layout.Button("Add", layout.Inset(0.05f))) 
			{
				if (currentlySelectedWorkers == null) currentlySelectedWorkers = new List<ThreadWorker>();
				currentlySelectedWorkers.Add(this);
				Debug.Log(name + " Log:\n\n" + PrintLog(currentlySelectedWorkers));
			}

			layout.Label("lock:" + lockWasTaken); //just to avoid warning that lockWasTaken never used
		}
	}
}
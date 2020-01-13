using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MapMagic
{

	//[Serializable] //custom generic classes are not serialized. Use ISerializationCallback instead
	public struct TupleSet <T1, T2> //: IEquatable<Tuple<T1,T2>>, IEqualityComparer
	{
		public T1 item1;
		public T2 item2;

		public TupleSet (T1 i1, T2 i2) { item1=i1; item2=i2; }

		//public override bool Equals (object obj) {  }
	}


	public struct TupleSet <T1, T2, T3>
	{
		public T1 item1;
		public T2 item2;
		public T3 item3;

		public TupleSet (T1 i1, T2 i2, T3 i3) { item1=i1; item2=i2; item3=i3; }
	}


	public class DictTuple <TKey, T1, T2>
	{
		private Dictionary<TKey, TupleSet<T1,T2>> dict = new Dictionary<TKey, TupleSet<T1, T2>>();

		public void Add (TKey key, T1 val1, T2 val2) { dict.Add(key, new TupleSet<T1,T2>(val1,val2)); }
		public void CheckAdd (TKey key, T1 val1, T2 val2, bool overwrite=false) 
		{ 
			if (!dict.ContainsKey(key)) dict.Add(key, new TupleSet<T1,T2>(val1,val2));
			else if (overwrite) dict[key] = new TupleSet<T1,T2>(val1,val2);
		}
		public int Count {get{ return dict.Count; }}
		public void Clear () { dict.Clear(); }
		public bool ContainsKey (TKey key) { return dict.ContainsKey(key); }
		public void Remove (TKey key) { dict.Remove(key); }
		public void CheckRemove (TKey key) { if (dict.ContainsKey(key)) dict.Remove(key); }
		
		public TupleSet<T1,T2> Get (TKey key) { return dict[key]; }
		public T1 GetVal1 (TKey key) { return dict[key].item1; }
		public T2 GetVal2 (TKey key) { return dict[key].item2; }
		public void Set (TKey key, T1 val1, T2 val2) { if (!dict.ContainsKey(key)) dict.Add(key, new TupleSet<T1,T2>(val1,val2)); else dict[key] = new TupleSet<T1,T2>(val1,val2); } //same as add with owerwrite
		public void SetVal1 (TKey key, T1 val) { TupleSet<T1,T2> tuple=dict[key]; tuple.item1=val; dict[key]=tuple; }
		public void SetVal2 (TKey key, T2 val) { TupleSet<T1,T2> tuple=dict[key]; tuple.item2=val; dict[key]=tuple; }
		
		public TupleSet<T1,T2> this[TKey key] { get {return dict[key];} set{dict[key]=value;} }

		public IEnumerable<TupleSet<T1,T2>> Values () { foreach (TupleSet<T1,T2> val in dict.Values) yield return val; }
	}


	public class DictTuple <TKey, T1, T2, T3>
	{
		private Dictionary<TKey, TupleSet<T1,T2,T3>> dict = new Dictionary<TKey, TupleSet<T1,T2,T3>>();

		public void Add (TKey key, T1 val1, T2 val2, T3 val3) { dict.Add(key, new TupleSet<T1,T2,T3>(val1,val2,val3)); }
		public void CheckAdd (TKey key, T1 val1, T2 val2, T3 val3, bool overwrite=false) 
		{ 
			if (!dict.ContainsKey(key)) dict.Add(key, new TupleSet<T1,T2,T3>(val1,val2,val3));
			else if (overwrite) dict[key] = new TupleSet<T1,T2,T3>(val1,val2,val3);
		}
		public int Count {get{ return dict.Count; }}
		public void Clear () { dict.Clear(); }
		public bool ContainsKey (TKey key) { return dict.ContainsKey(key); }
		public void Remove (TKey key) { dict.Remove(key); }
		public void CheckRemove (TKey key) { if (dict.ContainsKey(key)) dict.Remove(key); }
		
		public TupleSet<T1,T2,T3> Get (TKey key) { return dict[key]; }
		public T1 GetVal1 (TKey key) { return dict[key].item1; }
		public T2 GetVal2 (TKey key) { return dict[key].item2; }
		public T3 GetVal3 (TKey key) { return dict[key].item3; }
		public void Set (TKey key, T1 val1, T2 val2, T3 val3) { if (!dict.ContainsKey(key)) dict.Add(key, new TupleSet<T1,T2,T3>(val1,val2,val3)); else dict[key] = new TupleSet<T1,T2,T3>(val1,val2,val3); } //same as add with owerwrite
		public void SetVal1 (TKey key, T1 val) { TupleSet<T1,T2,T3> tuple=dict[key]; tuple.item1=val; dict[key]=tuple; }
		public void SetVal2 (TKey key, T2 val) { TupleSet<T1,T2,T3> tuple=dict[key]; tuple.item2=val; dict[key]=tuple; }
		public void SetVal3 (TKey key, T3 val) { TupleSet<T1,T2,T3> tuple=dict[key]; tuple.item3=val; dict[key]=tuple; }
		
		public TupleSet<T1,T2,T3> this[TKey key] { get {return dict[key];} set{dict[key]=value;} }
	}


	public class MultiDict <TKey, TValue>
	{
		public Dictionary<TKey, List<TValue>> dict = new Dictionary<TKey, List<TValue>>();

		public void Add (TKey key, TValue val) 
		{ 
			if (dict.ContainsKey(key)) dict[key].Add(val);
			else 
			{
				List<TValue> sub = new List<TValue>();
				sub.Add(val);
				dict.Add(key, sub);
			}
		}

		public IEnumerable<TValue> Items (TKey key)
		{
			if (!dict.ContainsKey(key)) yield break;
			List<TValue> sub = dict[key];

			int listCount = sub.Count;
			for (int i=0; i<listCount; i++)
				yield return sub[i];
		}

		public List<TValue> this[TKey key] 
		{
			get{ if (dict.ContainsKey(key)) return dict[key]; else return null; }
			set{ if (!dict.ContainsKey(key)) dict.Add(key,value); else dict[key] = value; }
		}
		
		public void Remove (TKey key, TValue val)
		{
			if (dict.ContainsKey(key))
				dict[key].Remove(val);
		}
		public void RemoveKey (TKey key) { dict.Remove(key); }

		public bool Contains (TKey key, TValue val)
		{
			if (!dict.ContainsKey(key)) return false;
			else if (dict[key].Contains(val)) return true;
			else return false;
		}
		public bool ContainsKey (TKey key) { return dict.ContainsKey(key); }

		public void Clear () { dict.Clear(); }	
		public int Count {get{ return dict.Count; }}
	}


	public class OrderedDict <TKey, TValue>
	{
		private Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
		private List<TupleSet<TKey,TValue>> list = new List<TupleSet<TKey,TValue>>();

		public void Add(TKey key, TValue val)
		{
			dict.Add(key, val);
			list.Add( new TupleSet<TKey,TValue>(key,val) );
		}

		public void Insert(int index, TKey key, TValue val)
		{
			if(index > list.Count || index < 0) throw new ArgumentOutOfRangeException("index", "Index Out Of Range (0-" + list.Count +"): " + index);

			dict.Add(key, val);
			list.Insert(index, new TupleSet<TKey,TValue>(key, val));
		}

		public int IndexOfKey(TKey key) //very slow operation
		{
			if(null == key) throw new ArgumentNullException("key");

			int listCount = list.Count;
			for (int i=0; i<listCount; i++)
			{
				if (Equals(key, list[i].item1)) return i;
			}

			return -1;
		}

		public TValue this[int index]
		{
			get { 
				if(index >= list.Count || index < 0) throw new ArgumentOutOfRangeException("index", "Index Out Of Range (0-" + list.Count +"): " + index);
				return list[index].item2; 
			}
			set {
				if(index >= list.Count || index < 0) throw new ArgumentOutOfRangeException("index", "Index Out Of Range (0-" + list.Count +"): " + index);

				TKey key = list[index].item1;

				list[index] = new TupleSet<TKey,TValue>(key,value);
				dict[key] = value;
			}
		}

		public TValue this[TKey key]
		{
			get { return dict[key]; }
			set {
				if(dict.ContainsKey(key))
				{
					dict[key] = value;
					list[ IndexOfKey(key) ] = new TupleSet<TKey,TValue>(key, value);
				}
				else Add(key, value);
			}
		}

		public void RemoveAt(int index)
		{
			if(index >= list.Count || index < 0) throw new ArgumentOutOfRangeException("index", "Index Out Of Range (0-" + list.Count +"): " + index);

			dict.Remove( list[index].item1 );
			list.RemoveAt(index);
		}

		public bool Remove(TKey key)
		{
			if(null == key) throw new ArgumentNullException("key");

			if (dict.ContainsKey(key))
			{
				dict.Remove(key);
				list.RemoveAt( IndexOfKey(key) );
				return true;
			}
			return false;
		}

		public void Clear()
		{
			dict.Clear();
			list.Clear();
		}
		
		public bool ContainsKey (TKey key) { return dict.ContainsKey(key); }
		public int Count {get{ return list.Count; }}
	}


	public class OrderedMultiDict <TKey, TValue>
	{
		private Dictionary<TKey, List<TValue>> dict = new Dictionary<TKey, List<TValue>>();
		private List<TupleSet<TKey,int,TValue>> list = new List<TupleSet<TKey,int,TValue>>();

		public void Insert (int index, TKey key, TValue val)
		{
			int numInSub;
			if (dict.ContainsKey(key)) 
			{
				List<TValue> sub = dict[key];
				numInSub = sub.Count;
				sub.Add(val);
			}
			else 
			{
				List<TValue> sub = new List<TValue>();
				sub.Add(val);
				dict.Add(key, sub);
				numInSub = 0;
			}

			if (index == list.Count) list.Add( new TupleSet<TKey,int,TValue>(key,numInSub,val) );
			else list.Insert( index, new TupleSet<TKey,int,TValue>(key,numInSub,val) );  //inserting to list end 2 times slower than add (but far way faster than insert in the beginning)
		}

		public void Add (TKey key, TValue val) { Insert(list.Count, key, val); }

		public int IndexOfKeyNum(TKey key, int num) //very slow operation
		{
			if(null == key) throw new ArgumentNullException("key");

			int listCount = list.Count;
			for (int i=0; i<listCount; i++)
			{
				if (Equals(key, list[i].item1) && list[i].item2==num) return i;
			}

			return -1;
		}

		public int FirstIndexOf (TValue val)
		{
			int listCount = list.Count;
			for (int i=0; i<listCount; i++)
			{
				if (Equals(val, list[i].item3)) return i;
			}
			return -1;
		}

		public TValue this[int index]
		{
			get { 
				if(index >= list.Count || index < 0) throw new ArgumentOutOfRangeException("index", "Index Out Of Range (0-" + list.Count +"): " + index);
				return list[index].item3; 
			}
			set {
				if(index >= list.Count || index < 0) throw new ArgumentOutOfRangeException("index", "Index Out Of Range (0-" + list.Count +"): " + index);

				TKey key = list[index].item1;
				int numInSub = list[index].item2;

				list[index] = new TupleSet<TKey,int,TValue>(key,numInSub,value);
				dict[key][numInSub] = value;
			}
		}

		public TValue this[TKey key, int num]
		{
			get { return dict[key][num]; }
			set {
				if(dict.ContainsKey(key))
				{
					List<TValue> sub = dict[key];

					if(num >= sub.Count || num < 0) throw new ArgumentOutOfRangeException("num", "Index Out Of Range (0-" + sub.Count +"): " + num);

					if (num == sub.Count) Add(key,value); //adding right after the last
					else
					{
						dict[key][num] = value;
						list[ IndexOfKeyNum(key,num) ] = new TupleSet<TKey,int,TValue>(key,num,value);
					}
				}
				else if (num == 0) Add(key, value);
				else throw new ArgumentException("key", "Key Not Found: " + key);
			}
		}

		public IEnumerable<TValue> Values (TKey key)
		{
			if (!dict.ContainsKey(key)) yield break;
			
			List<TValue> sub = dict[key];

			int subCount = sub.Count;
			for (int i=0; i<subCount; i++)
				yield return sub[i];
		}

		public List<TValue> SubList (TKey key)
		{
			if (!dict.ContainsKey(key)) return null;
			return dict[key];
		}

		public void RemoveAt(int index)
		{
			if(index >= list.Count || index < 0) throw new ArgumentOutOfRangeException("index", "Index Out Of Range (0-" + list.Count +"): " + index);

			TKey key = list[index].item1;
			int num = list[index].item2;

			List<TValue> sub = dict[key];
			sub.RemoveAt(num);
			if (sub.Count==0) dict.Remove(key);

			list.RemoveAt(index);

			//shifting all of the nums
			int listCount = list.Count;
			for (int i=0; i<listCount; i++)
			{
				TKey ikey = list[i].item1;
				int inum = list[i].item2;
				if (Equals(key, ikey) && inum>=num)
					list[i] = new TupleSet<TKey,int,TValue>(ikey, inum-1, list[i].item3);
			}
		}

		public bool RemoveKey (TKey key)
		{
			if(null == key) throw new ArgumentNullException("key");

			if (dict.ContainsKey(key))
			{
				dict.Remove(key);
				
				for (int i=list.Count-1; i>=0; i--)
					if (Equals(key, list[i].item1)) list.RemoveAt(i);

				return true;
			}
			return false;
		}

		public bool RemoveValue (TValue val) 
		{ 
			int i = FirstIndexOf(val); 
			if (i==-1) return false;
			RemoveAt(i); 
			return true; 
		}

		public void Clear()
		{
			dict.Clear();
			list.Clear();
		}
		
		public bool ContainsKey (TKey key) { return dict.ContainsKey(key); }
		public int Count {get{ return list.Count; }}
		public int CountAtKey (TKey key) { if (!dict.ContainsKey(key)) return -1; else return dict[key].Count; }
	}
}
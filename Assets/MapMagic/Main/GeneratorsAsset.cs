using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using MapMagic;

namespace MapMagic
{
	[System.Serializable]
	public class GeneratorsAsset : ScriptableObject , ISerializationCallbackReceiver
	{
		public Generator[] list = new Generator[0];
		//public OrderedMultiDict<System.Type, Generator> dict; //TODO for v2
		
		public int oldId = 0;
		public int oldCount = 0;

		[System.NonSerialized] public Layout layout;
		public Vector2 guiScroll = new Vector2(0,0);
		public float guiZoom = 1; //TODO: maybe serialize layout? Just mark all unnecessary fields as NonSerialized

		#region Generators Enumerables

			public IEnumerable<T> GeneratorsOfType<T> (bool onlyEnabled=true, bool checkBiomes=true) where T : Generator
			{
				for (int i=0; i<list.Length; i++)
				{
					Generator gen = list[i];
					if (gen==null) continue; //unknown custom type
					if (onlyEnabled && !gen.enabled) continue;
					if (list[i] is T) 
					{
						gen.biome = null;
						yield return (T)gen;
					}
				}

				if (checkBiomes) for (int i=0; i<list.Length; i++)
				{
					Generator gen = list[i];
					if (gen==null) continue; //unknown custom type
					if (onlyEnabled && !gen.enabled) continue;

					if (gen is Biome) 
					{
						Biome biome = (Biome)gen;
						if (biome.data!=null && biome.mask.linkGen!=null) 
							foreach (T biomeGen in biome.data.GeneratorsOfType<T>(onlyEnabled, true)) 
							{
								biomeGen.biome = biome;
								yield return biomeGen;
							}
					}
				}
			}

			/*public IEnumerable<Generator> GeneratorsOfType (System.Type type, bool onlyEnabled=true, bool checkBiomes=true)
			{
				for (int i=0; i<list.Length; i++)
				{
					Generator gen = list[i];
					if (gen==null) continue; //unknown custom type
					if (onlyEnabled && !gen.enabled) continue;
					if (list[i].GetType() == type) 
					{
						gen.biome = null;
						yield return gen;
					}
				}

				if (checkBiomes) for (int i=0; i<list.Length; i++)
				{
					Generator gen = list[i];
					if (gen==null) continue; //unknown custom type
					if (onlyEnabled && !gen.enabled) continue;

					if (gen is Biome) 
					{
						Biome biome = (Biome)gen;
						if (biome.data!=null && biome.mask.linkGen!=null) 
							foreach (Generator biomeGen in biome.data.GeneratorsOfType(type, onlyEnabled, true)) 
							{
								biomeGen.biome = biome;
								yield return biomeGen;
							}
					}
				}
			}

			public bool HasGenerator (System.Type type, bool onlyEnabled=true, bool checkBiomes=true)
			{
				foreach (Generator gen in GeneratorsOfType(type, onlyEnabled:onlyEnabled, checkBiomes:checkBiomes))
					{ return true; }
				return false;
			}

			public T AnyGenerator<T> (bool onlyEnabled=true, bool checkBiomes=true) where T : Generator //TODO: make non-generic
			{
				foreach (T gen in GeneratorsOfType<T>(onlyEnabled:onlyEnabled, checkBiomes:checkBiomes))
					{ return gen; }
				return null;
			}*/

			//TODO for v2
			/*public IEnumerable<T> GeneratorsOfType<T> (bool onlyEnabled=true, bool checkBiomes=true) where T : Generator
			{
				//main generators
				List<Generator> gensList = dict.SubList( typeof(T) );

				int gensListCount = gensList.Count;
				for (int i=0; i<gensListCount; i++)
				{
					Generator gen = gensList[i];
					if (onlyEnabled && !gen.enabled) continue;

					gen.biome = null;
					yield return (T)gen;
				}

				//biomes
				if (checkBiomes) 
				{
					List<Generator> biomesList = dict.SubList( typeof(Biome) );

					int biomesCount = biomesList.Count;
					for (int i=0; i<biomesCount; i++)
					{
						Biome biome = (Biome)(biomesList[i]);
						if (onlyEnabled && !biome.enabled) continue;

						foreach (T biomeGen in biome.data.GeneratorsOfType<T>(onlyEnabled, true)) 
						{
							biomeGen.biome = biome;
							yield return biomeGen;
						}
					}
				}
			}*/

			/*public IEnumerable<OutputGenerator> OutputGenerators (bool onlyEnabled=true, bool checkBiomes=true) //TODO: idea make IOutput child class to Generator
			{
				for (int i=0; i<list.Length; i++)
				{
					Generator gen = list[i];
					if (gen==null) continue;
					if (onlyEnabled && !gen.enabled) continue;
					if (list[i] is OutputGenerator) yield return (OutputGenerator)gen;
				}

				if (checkBiomes) for (int i=0; i<list.Length; i++)
				{
					Generator gen = list[i];
					if (gen==null) continue;
					if (onlyEnabled && !gen.enabled) continue;

					if (gen is Biome) 
					{
						Biome biome = (Biome)gen;
						if (biome.data!=null && biome.mask.linkGen!=null) 
							foreach (OutputGenerator biomeGen in biome.data.OutputGenerators(onlyEnabled, true)) yield return biomeGen;
					}
				}
			}*/




			/*public HashSet<Type> GetExistingOutputTypes (bool onlyEnabled=true, bool checkBiomes=true) //used to purge the others
			{
				HashSet<Type> existingOutputs = new HashSet<Type>();
			
				for (int i=0; i<list.Length; i++)
				{
					Generator gen = list[i];
					if (gen==null) continue;
					if (onlyEnabled && !gen.enabled) continue;
					if (list[i] is Generator.IOutput)
					{
						Type type = list[i].GetType();
						if (!existingOutputs.Contains(type)) existingOutputs.Add(type);
					} 
				}

				if (checkBiomes) for (int i=0; i<list.Length; i++)
				{
					Generator gen = list[i];
					if (gen==null) continue;
					if (onlyEnabled && !gen.enabled) continue;

					if (gen is Biome) 
					{
						Biome biome = (Biome)gen;
						if (biome.data!=null && biome.mask.linkGen!=null) 
							existingOutputs.UnionWith( biome.data.GetExistingOutputTypes(onlyEnabled, true) );
					}
				}

				return existingOutputs;
			}*/

		#endregion


		#region Prepare/Calculate/Apply

			public void Prepare (Chunk chunk)
			{
				//calling before generate event
				if (MapMagic.instance != null) 
				{
					//foreach (Chunk chunk in MapMagic.instance.chunks.All())
						MapMagic.CallOnPrepareStarted(chunk.terrain);
				}
				
				#if VOXELAND
				foreach (VoxelandOutput tin in GeneratorsOfType<VoxelandOutput>()) { VoxelandOutput.voxeland = FindObjectOfType<Voxeland5.Voxeland>(); break; } //TODO: test if foreach really needed here
				foreach (VoxelandObjectsOutput tin in GeneratorsOfType<VoxelandObjectsOutput>()) { VoxelandObjectsOutput.voxeland = FindObjectOfType<Voxeland5.Voxeland>(); break; }
				foreach (VoxelandGrassOutput tin in GeneratorsOfType<VoxelandGrassOutput>()) { VoxelandOutput.voxeland = FindObjectOfType<Voxeland5.Voxeland>(); break; }
				#endif
				foreach (TextureInput tin in GeneratorsOfType<TextureInput>()) tin.CheckLoadTexture(); 
				#if VEGETATION_STUDIO
				foreach (VegetationStudioOutput vsout in GeneratorsOfType<VegetationStudioOutput>()) vsout.CheckAddComponent(chunk);
				#endif
				//more will follow
			}

			public void Calculate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null) //TODO: is "Calculate" really needed or maybe do that in chunk's ThreadFn?
			{
				HashSet<System.Type> changedTypes = new HashSet<Type>(); //to process output types

				Generate (rect, results, terrainSize, seed, ref changedTypes, stop);

				Process (rect, results, this, terrainSize, changedTypes, stop);
			}
			public void Calculate (int offsetX, int offsetZ, int size, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
				{Calculate(new CoordRect(offsetX,offsetZ,size,size), results, terrainSize, seed, stop); }

			public void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, ref HashSet<System.Type> changedTypes, Func<float,bool> stop = null)
			{
				//clearing final nodes
				for (int g=0; g<list.Length; g++)
				{
					Generator gen = list[g];
					if (!gen.enabled) continue;

					//processing only output, biome or preview
					if ( !(gen is OutputGenerator)  &&  !(gen is Biome)  &&  gen != Preview.previewGenerator ) continue;
					
					CheckClearRecursive(gen, results);

					if (stop!=null && stop(0)) return;
				}


				//populating changed types (before actually generating, otherwise texture/rtp/megasplat outputs could be generated by objects or grass generators without process and apply)
				for (int g=0; g<list.Length; g++)
				{
					Generator gen = list[g];
					if (!gen.enabled) continue;

					//skipping ready generators
					if (results.ready.Contains(gen)) continue;

					//processing only output, biome or preview
					if (!(gen is OutputGenerator)) continue; //no need to check for biome and preview here
					
					//enqueue changed type
					if (!changedTypes.Contains(gen.GetType())) changedTypes.Add(gen.GetType());
				}
				if (stop!=null && stop(0)) return;


				/*//adding heights to changed type if results have no heights matrix
				if (results.heights == null && !changedTypes.Contains(typeof(HeightOutput))) //add heights.IsEmpty test if heights is cleared but not nulled in results.Clear
				{
					for (int g=0; g<list.Length; g++)
					{
						Generator gen = list[g];
						if (!gen.enabled || !(gen is HeightOutput)) continue;
						if (results.ready.Contains(gen)) results.ready.Remove(gen);
					}
					changedTypes.Add(typeof(HeightOutput));
				}
				if (stop!=null && stop(0)) return;


				//clearing objects and trees if heights are cleared (better do it in generate, in process it will not work with "not keep results")
				if (changedTypes.Contains(typeof(HeightOutput)))
				{
					for (int g=0; g<list.Length; g++)	
					{
						Generator gen = list[g];
						if (!gen.enabled) continue;
								
						if (gen is ObjectOutput || gen is TreesOutput)
						{
							if (results.ready.Contains(gen)) results.ready.Remove(gen);
							if (!changedTypes.Contains(gen.GetType())) changedTypes.Add(gen.GetType()); //enqueue changed type
						}
					}
				}
				if (stop!=null && stop(0)) return;*/


				//generate final nodes
				for (int g=0; g<list.Length; g++)
				{
					Generator gen = list[g];
					if (!gen.enabled) continue;

					//processing only output, biome or preview
					if ( !(gen is OutputGenerator)  &&  !(gen is Biome)  &&  gen != Preview.previewGenerator ) continue;

					//generator has changed - generate
					if (!results.ready.Contains(gen))
						GenerateWithPriors(gen, rect, results, terrainSize, seed, stop);


					//generate biomes recursively
					if (gen is Biome)
					{
						Biome biome = (Biome)gen;
						if (biome.data==null) continue; 

						//add to changed types all of biome outputs
						//note that this works only on changed biomes, we have results.ready.Contains test earlier
						//all changed biomes - regardless of their mask. This should work when disconnecting biome
						for (int ig=0; ig<biome.data.list.Length; ig++)
						{
							Generator innerGen = biome.data.list[ig];
							if (!innerGen.enabled) continue;

							if (innerGen is OutputGenerator && !changedTypes.Contains(innerGen.GetType())) 
								changedTypes.Add(innerGen.GetType());
							
							//mark biome as changed to perform add changed types recursively
							//if (innerGen is OutputGenerator && results.ready.Contains(innerGen)) 
							//	results.ready.Remove(innerGen);
						}

						//generate biome
						object maskBox = biome.mask.GetObject(results);
						if (biome.data!=null && maskBox != null && !((Matrix)maskBox).IsEmpty()) //check if biome has data and mask
						{
							biome.data.Generate (rect, results, terrainSize, seed, ref changedTypes, stop);
						}
					}

					//TODO: still recursive biomes do not work. Only one level is possible

					if (stop!=null && stop(0)) return;
				}


				//generate biomes recursively (before adding heights, objects and trees to changed types)
				/*for (int g=0; g<list.Length; g++)
				{
					Biome biome = list[g] as Biome;
					if (biome == null || !biome.enabled || biome.data==null) continue; 

					//skip if biome has no mask
					object maskBox = biome.mask.GetObject(results);
					if (maskBox == null) continue;
					Matrix mask = (Matrix)maskBox;
					if (mask.IsEmpty()) continue;

					//add to changed types all of biome outputs if it (i.e. it's mask) has changed
					if (!results.ready.Contains(biome))
					{
						for (int ig=0; ig<biome.data.list.Length; ig++)
						{
							Generator innerGen = biome.data.list[ig];
							if (!innerGen.enabled) continue;

							if (innerGen is OutputGenerator && !changedTypes.Contains(innerGen.GetType())) 
								changedTypes.Add(innerGen.GetType());
							
							//mark biome as changed to perform add changed types recursively
							if (innerGen is OutputGenerator && results.ready.Contains(innerGen)) 
								results.ready.Remove(innerGen);
						}
					}
					
					biome.data.Generate (rect, results, terrainSize, seed, ref changedTypes, worker);

					if (stop!=null && stop(0)) return;
				}*/

			}

			public void Process (CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, HashSet<System.Type> changedTypes, Func<float,bool> stop = null)
			{
				//filling static methods hashes
				Dictionary<Type, Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>>> processFunctionsCache = new Dictionary<Type, Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>>>();
				
				processFunctionsCache.Add(typeof(TreesOutput), TreesOutput.Process); //adding trees and objects functions now just to avoid adding them in the next block
				processFunctionsCache.Add(typeof(ObjectOutput), ObjectOutput.Process);
				processFunctionsCache.Add(typeof(VegetationStudioOutput), VegetationStudioOutput.Process);
				
				foreach (OutputGenerator outGen in GeneratorsOfType<OutputGenerator>(onlyEnabled:false, checkBiomes:true))
				{
					Type type = outGen.GetType();
					if (!processFunctionsCache.ContainsKey(type))
					{
						OutputGenerator o = outGen as OutputGenerator;
						processFunctionsCache.Add(type, o.GetProces());
					}
				}
				
				//adding trees/objects types to process and apply if height changed
				if (changedTypes.Contains(typeof(HeightOutput)))
				{
					if (!changedTypes.Contains(typeof(TreesOutput))) changedTypes.Add(typeof(TreesOutput)); //TODO: don't trees change their height like grass?
					if (!changedTypes.Contains(typeof(ObjectOutput))) changedTypes.Add(typeof(ObjectOutput));

					if (!changedTypes.Contains(typeof(VegetationStudioOutput)))
						foreach(VegetationStudioOutput vetOut in gens.GeneratorsOfType<VegetationStudioOutput>(true,true))  //enqueuing only if graph has generators of this type
						{
							changedTypes.Add(typeof(VegetationStudioOutput));
							break;
						}
				}

				//debug timer
				#if WDEBUG
				System.Diagnostics.Stopwatch timer = null;
				timer = new System.Diagnostics.Stopwatch();
				#endif
				
				lock (Generator.guiProcessTime) Generator.guiProcessTime.Clear();

				//processing height output first
				if (changedTypes.Contains(typeof(HeightOutput)))
				{
					#if WDEBUG
					if (timer!=null) timer.Start();
					#endif
						
					HeightOutput.Process(rect, results, this, terrainSize, stop);
						
					#if WDEBUG
					if (timer!=null)
					{
						timer.Stop();
						lock (Generator.guiProcessTime) //otherwise it can try adding same key twice
							Generator.guiProcessTime.CheckAdd(typeof(HeightOutput), (int)timer.ElapsedMilliseconds);
						timer.Reset();
					}
					#endif

					changedTypes.Remove(typeof(HeightOutput));
				}

				//processing splat output before MegaSplat and RTP
				if (changedTypes.Contains(typeof(SplatOutput)))
				{
					#if WDEBUG
					if (timer!=null) timer.Start();
					#endif
						
					SplatOutput.Process(rect, results, this, terrainSize, stop);
					
					#if WDEBUG	
					if (timer!=null)
					{
						timer.Stop();
						lock (Generator.guiProcessTime) //otherwise it can try adding same key twice
							Generator.guiProcessTime.CheckAdd(typeof(SplatOutput), (int)timer.ElapsedMilliseconds);
						timer.Reset();
					}
					#endif

					changedTypes.Remove(typeof(SplatOutput));
				}

				if (changedTypes.Contains(typeof(TexturesOutput)))
				{
					TexturesOutput.Process(rect, results, this, terrainSize, stop);
					changedTypes.Remove(typeof(TexturesOutput));
				}

				//processing all the others
				foreach (Type type in changedTypes)
				{
					if (!type.IsSubclassOf(typeof(OutputGenerator))) continue; //if preview or biome
					
					if (!processFunctionsCache.ContainsKey(type)) Debug.LogError("No such type in process cache: " + type);
					Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> processAction = processFunctionsCache[type];
					if (processAction==null) continue; //biomes have null process

					#if WDEBUG
					if (timer!=null) timer.Start();
					#endif

					processAction(rect, results, this, terrainSize, stop);
						
					#if WDEBUG
					if (timer!=null)
					{
						timer.Stop();
						lock (Generator.guiProcessTime) //otherwise it can try adding same key twice
							Generator.guiProcessTime.CheckAdd(type, (int)timer.ElapsedMilliseconds);
						timer.Reset();
					}
					#endif
				}
			}


			public void ApplyNow (CoordRect rect, Chunk.Results results, Terrain terrain, Func<float,bool> stop = null, bool purge= true)
			{
				IEnumerator e = Apply(rect,results,terrain,stop,purge);
				while (e.MoveNext());
			}

			public IEnumerator Apply (CoordRect rect, Chunk.Results results, Terrain terrain, Func<float,bool> stop = null, bool purge=true)
			{
				//filling static methods hashes
				Dictionary<Type, Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator>> applyFunctionsCache = new Dictionary<Type, Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator>>();
				
				applyFunctionsCache.Add(typeof(TreesOutput), TreesOutput.Apply); 
				applyFunctionsCache.Add(typeof(ObjectOutput), ObjectOutput.Apply);
				applyFunctionsCache.Add(typeof(VegetationStudioOutput), VegetationStudioOutput.Apply);
				
				foreach (OutputGenerator outGen in GeneratorsOfType<OutputGenerator>(onlyEnabled:false, checkBiomes:true))
				{
					Type type = outGen.GetType();
					if (!applyFunctionsCache.ContainsKey(type))
					{
						OutputGenerator o = outGen as OutputGenerator;
						applyFunctionsCache.Add(type, o.GetApply());
					}
				}

				//calling before-apply event
				MapMagic.CallOnGenerateCompleted(terrain); //if (MapMagic.OnGenerateCompleted != null) MapMagic.OnGenerateCompleted(terrain);

				//debug timer
				#if WDEBUG
				System.Diagnostics.Stopwatch timer = null;
				timer = new System.Diagnostics.Stopwatch();
				#endif
			
				lock (Generator.guiApplyTime) Generator.guiApplyTime.Clear();

				//apply
				foreach (KeyValuePair<Type,object> kvp in results.apply)
				{
					System.Type type = kvp.Key;
					object dataBox = kvp.Value;

					results.nonEmpty.CheckAdd(type);

					//get apply function
					if (!applyFunctionsCache.ContainsKey(type)) 
						Debug.LogError("No such type in apply cache: " + type);

					Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> applyFn = applyFunctionsCache[type];
					if (applyFn == null) continue; //for biomes

					//creating enumerator
					IEnumerator e = null;
					e = applyFn(rect, terrain, dataBox, stop);

					//apply enumerator
					#if WDEBUG
					if (timer!=null) timer.Start();
					#endif

					while (e.MoveNext()) 
					{				
						if (terrain==null) yield break; //guard in case max terrains count < actual terrains: terrain destroyed or still processing
						yield return null;
					}

					#if WDEBUG
					if (timer!=null)
					{
						timer.Stop();
						lock (Generator.guiApplyTime) //otherwise it can try adding same key twice
							Generator.guiApplyTime.CheckAdd(type, (int)timer.ElapsedMilliseconds);
						timer.Reset();
					}
					#endif
				}

				//purging unused outputs
				//now purged on disable
				/*if (purge)
				{
					HashSet<Type> existingOutputs = new HashSet<Type>();
					foreach (OutputGenerator outGen in OutputGenerators())
						if (!existingOutputs.Contains(outGen.GetType())) existingOutputs.Add(outGen.GetType());

					foreach (KeyValuePair<Type, Action<CoordRect,Terrain>> kvp in purgeFunctionsCache)
					{
						Type type = kvp.Key;
						if (!existingOutputs.Contains(type)) 
						{
							Action<CoordRect,Terrain> purgeFn = kvp.Value;
							if (purgeFn==null) continue; //for biomes, they have null purge
							purgeFn(rect,terrain);
						}
					}
				}*/

				//creating initial texture if splatmap count is 0 - just to look good
				

				#if UNITY_2018_3_OR_NEWER
				if (terrain.terrainData.terrainLayers.Length == 0) TexturesOutput.Purge(rect,terrain);
				#else
				if (terrain.terrainData.splatPrototypes.Length == 0) SplatOutput.Purge(rect,terrain);
				#endif

				//clearing intermediate results
				results.apply.Clear();
				if (MapMagic.instance==null || !MapMagic.instance.isEditor || !MapMagic.instance.saveIntermediate) { results.results.Clear(); results.ready.Clear(); } //this should be done in thread, but thread has no access to isPlaying
				//TODO use dispose for that
				//TODO does not work in Voxeland

				//enabling terrain if it was disabled
				//if (!terrain.gameObject.activeSelf) terrain.gameObject.SetActive(true);  //enabling/disabling is now in update control

				//calling after-apply event
				MapMagic.CallOnApplyCompleted(terrain);
			}
		#endregion


		#region Generators fns

			//connection states
			public static bool CanConnect (Generator.Output output, Generator.Input input) { return output.type == input.type; } //temporary out of order, before implementing resolutions


			public bool CheckDependence (Generator prior, Generator post)
			{
				foreach (Generator.Input input in post.Inputs())
				{
					if (input==null || input.linkGen==null) continue;
					if (prior == input.linkGen) return true;
					if (CheckDependence(prior,input.linkGen)) return true;
				}
				return false;
			}

			public void CheckClearRecursive (Generator gen, Chunk.Results tw) //checks if prior generators were clearied, and if they were - clearing this one
			{
				foreach (Generator.Input input in gen.Inputs())
				{
					if (input == null) continue; //in case a previous version of the generator loaded (without that input). TODO create a new generator here when I'll switch to generics
					if (input.linkGen==null) continue;

					//recursive first
					CheckClearRecursive(input.linkGen, tw);

					//checking if clear
					if (!tw.ready.Contains(input.linkGen))
					{
						if (tw.ready.Contains(gen)) tw.ready.Remove(gen);
						//break; do not break, go on checking in case of branching-then-connecting
					}
				}
			}

			public void GenerateWithPriors (Generator gen, CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
			{
				//generating input generators
				foreach (Generator.Input input in gen.Inputs())
				{
					if (input.linkGen==null) continue;
					if (stop!=null && stop(0)) return; //before entry stop
					GenerateWithPriors(input.linkGen, rect, results, terrainSize, seed, stop);
				}

				if (stop!=null && stop(0)) return; //before generate stop for time economy

				//generating this
				if (!results.ready.Contains(gen))
				{
					#if WDEBUG
					System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch(); timer.Start();
					#endif

					gen.Generate(rect, results, terrainSize, seed, stop);
					if (stop==null || !stop(0)) results.ready.Add(gen);

					#if WDEBUG
					timer.Stop(); gen.guiGenerateTime = (int)timer.ElapsedMilliseconds;
					#endif
				}
			}

			public void OnDisableGenerator (Generator gen) //called by window AFTER generator has been disabled
			{
				//removing from preview
				if (Preview.previewGenerator == gen) Preview.Clear();

				//forcing generate/purge for an output
				if (gen is OutputGenerator)
				{
					//get MapMagic
					IMapMagic mapMagic = null;
					if (MapMagic.instance != null) mapMagic = MapMagic.instance;
					#if VOXELAND
					if (Voxeland5.Voxeland.instances != null && Voxeland5.Voxeland.instances.Count != 0 && Voxeland5.Voxeland.instances.Any() != null)
						mapMagic = Voxeland5.Voxeland.instances.Any();
					#endif

					//System.Type genType = gen.GetType();
					bool hasGenType = false; //is there generator of the same type in graph (and biomes)?

					//if there are any other outputs of the same type - clearing them and forcing generate
					foreach (OutputGenerator anyOut in GeneratorsOfType<OutputGenerator>(onlyEnabled:true, checkBiomes:true))
					{
						//if (anyOut.GetType() != genType) continue;
						//if (!anyOut.GetType() ) continue;
						
						hasGenType = true;

						if (mapMagic != null)
						{
							mapMagic.ClearResults(anyOut); 
							mapMagic.Generate();
						}
					}

					//if there are no generators of the same type - purging it
					if (!hasGenType)
					{
						OutputGenerator outGen = gen as OutputGenerator;
						

						if (mapMagic != null && mapMagic is MapMagic && MapMagic.instance != null)
						{
							Action<CoordRect, Terrain> purgeAction = outGen.GetPurge();

							if (purgeAction != null)
								foreach (Chunk chunk in MapMagic.instance.chunks.All())
									purgeAction(chunk.rect, chunk.terrain);
						}

						#if VOXELAND
						if (mapMagic != null && mapMagic is Voxeland5.Voxeland && Voxeland5.Voxeland.instances.Count != 0)
						{
							foreach (Voxeland5.Voxeland v in Voxeland5.Voxeland.instances)
							{
								if (v.data==null || v.data.generator==null || v.data.generator.mapMagicGens==null) continue;

								foreach (Voxeland5.Data.Area area in v.data.areas.All()) 
								{
									if (outGen is VoxelandOutput) area.ClearLand();
									if (outGen is VoxelandObjectsOutput) area.ClearObjects();
									if (outGen is VoxelandGrassOutput) area.ClearGrass();
								}
							}
						}
						#endif
					}
				}
			}

			public void DeleteGenerator (Generator gen)
			{
				//disabling generator
				gen.enabled = false;
				OnDisableGenerator(gen);
				
				//removing group members if it is group
				#if UNITY_EDITOR
				if (gen is Group)
				{
					int dialogResult = UnityEditor.EditorUtility.DisplayDialogComplex("Remove Containing Generators", "Do you want to remove a contaning generators as well?", "Remove Generators", "Remove Group Only", "Cancel");
					if (dialogResult==2) return; //cancel
					if (dialogResult==0) //generators
					{
						Group group = gen as Group;
						group.Populate(this);
						for (int g=group.generators.Count-1; g>=0; g--) DeleteGenerator(group.generators[g]);
					}
				}
				#endif

				//unlinking and removing it's reference in inputs and outputs
				UnlinkGenerator(gen);

				//removing from array
				ArrayTools.Remove(ref list, gen);

				//removing from preview
				if (Preview.previewGenerator == gen) Preview.Clear();

				//force regenerate if it was an output
				//if (gen is OutputGenerator && MapMagic.instance!=null)
				//{
				//	MapMagic.instance.ClearResults(); //TODO reset only dependent generators //for (int g=0; g<gens.list.Length; g++) if (gens.CheckDependence(gen,gens.list[g])) mapMagic.ClearResults(gens.list[g]);
				//	MapMagic.instance.Generate();
				//}
			}

			public void UnlinkGenerator (Generator gen, bool unlinkGroup=false)
			{
				//unlinking
				foreach (Generator.Input input in gen.Inputs()) { if (input != null) input.Unlink(); }

				//removing it's reference in inputs and outputs
				for (int g=0; g<list.Length; g++)
					foreach (Generator.Input input in list[g].Inputs())
						if (input != null && input.linkGen == gen) input.Unlink();
			
				//unlinking group
				Group grp = gen as Group;
				if (grp != null && unlinkGroup)
				{
					for (int g=0; g<list.Length; g++)
					{
						//if generator in group - unlinking it from non-group gens
						if (grp.guiRect.Contains(list[g].guiRect)) 
							foreach (Generator.Input input in list[g].Inputs())
								if (!grp.guiRect.Contains(input.linkGen.guiRect)) input.Unlink();

						//if generator not in group - unlinking it from group gens
						if (!grp.guiRect.Contains(list[g].guiRect)) 
							foreach (Generator.Input input in list[g].Inputs())
								if (grp.guiRect.Contains(input.linkGen.guiRect)) input.Unlink();
					} 
				}
			}


		#endregion

		/*public GeneratorsAsset Clone ()
		{
			GeneratorsAsset newGens = ScriptableObject.Instantiate<GeneratorsAsset>(
			copyGens = (Generator[])CustomSerialization.DeepCopy(MapMagic.instance.guiGens.list);
		}*/


		#region GUI Analogs
		//TODO: used only in MMWindow, do we need them here?

		public Generator CreateGenerator (System.Type type, Vector2 guiPos=new Vector2())
		{
			Generator gen = (Generator)System.Activator.CreateInstance(type);
 
			gen.guiRect.x = guiPos.x - gen.guiRect.width/2;
			gen.guiRect.y = guiPos.y - 10;
			if (gen is Group)
			{
				gen.guiRect.width = 300;
				gen.guiRect.height = 200;
			}

			//adding to outputs
//			if (gen is IOutput && GetGenerator(type) != null) 
//				{ Debug.LogError("MapMagic: Trying to add Output Generator while it already present in generators list"); return null; }
					
			//adding to list
			ArrayTools.Add(ref list, createElement:() => gen);

			return gen;
		}




		public Generator[] SmartDuplicateGenerators (Generator gen, bool appendToList=true) //generator duplicate in an array (or multiple gens if group. Or all gens if null)
		{
			Generator[] copyGens = null;

			//saving all gens if clicked to background
			if (gen == null) copyGens = (Generator[])CustomSerialization.DeepCopy(list);
				
			//saving group
			else if (gen is Group)
			{
				Group grp = (Group)gen;
				//Generator[] gens = MapMagic.instance.guiGens.list;

				//creating a list of group children (started with a group itself)
				List<Generator> gensList = new List<Generator>();
				gensList.Add(gen);
				for (int g=0; g<list.Length; g++)
					if (grp.guiRect.Contains(list[g].guiRect)) gensList.Add(list[g]);

				//copying group children
				copyGens = (Generator[])CustomSerialization.DeepCopy(gensList.ToArray());
					
				//unlinking children from out-of-group generators
				HashSet<Generator> copyGensHash = new HashSet<Generator>();
				for (int g=0; g<copyGens.Length; g++) copyGensHash.Add(copyGens[g]);
				for (int g=0; g<copyGens.Length; g++)
					foreach (Generator.Input input in copyGens[g].Inputs())
					{
						if (input.link == null) continue;
						if (!copyGensHash.Contains(input.linkGen)) input.Unlink();
					}
			}

			//single generator
			else 
			{
				Generator copyGen = (Generator)CustomSerialization.DeepCopy(gen);
				foreach (Generator.Input input in copyGen.Inputs()) { if (input != null) input.Unlink(); }
				copyGens = new Generator[] { copyGen };
			}

			if (appendToList)
			{
				for (int i=copyGens.Length-1; i>=0; i--) copyGens[i].guiRect.position += new Vector2(0, gen.guiRect.height + 10); //offset them a bit to prevent overlaping
				ArrayTools.Append(ref list, copyGens);
			}

			return copyGens;
		}

		public void ExportGenerator (Generator gen, Vector2 pos, string path=null) //if path is defined it will export generator using it
		{
			#if UNITY_EDITOR
			if (path==null) path= UnityEditor.EditorUtility.SaveFilePanel(
						"Export Nodes",
						"",
						"MapMagicExport.nodes", 
						"nodes");
			if (path==null || path.Length==0) return;

			Generator[] saveGens = SmartDuplicateGenerators(gen, appendToList:false);
			if (gen!=null) for (int i=0; i<saveGens.Length; i++) saveGens[i].guiRect.position -= gen.guiRect.position;

			//preparing serialization arrays
			List<string> classes = new List<string>();
			List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
			List<object> references = new List<object>();
			List<float> floats = new List<float>();

			//saving
			CustomSerialization.WriteClass(saveGens, classes, objects, floats, references);
			using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path))
				writer.Write(CustomSerialization.ExportXML(classes, objects, floats));

			#endif		
		}

		public Generator[] ImportGenerator (Vector2 pos, bool appendToList=true)
		{
			#if UNITY_EDITOR
			string path= UnityEditor.EditorUtility.OpenFilePanel(
						"Import Nodes",
						"", 
						"nodes");
			if (path==null || path.Length==0) return null;

			//preparing serialization arrays
			List<string> classes = new List<string>();
			List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
			List<object> references = new List<object>();
			List<float> floats = new List<float>();

			//loading
			System.IO.StreamReader reader = new System.IO.StreamReader(path);
			CustomSerialization.ImportXML(reader.ReadToEnd(), out classes, out objects, out floats);
			Generator[] loadedGens = (Generator[])CustomSerialization.ReadClass(0, classes, objects, floats, references);

			//offset 
			for (int i=loadedGens.Length-1; i>=0; i--) loadedGens[i].guiRect.position += pos;

			if (appendToList) ArrayTools.Append(ref list, loadedGens);

			return loadedGens;
			
			#else
			return null;
			#endif
		}

		public void ClearGenerators ()
		{
			list = new Generator[0];
			//outputs = new Generator[0];
		}



		public void SortGroups ()
		{
			for (int i=list.Length-1; i>=0; i--)
			{
				Generator grp = list[i];
				if (!(grp is Group)) continue;

				for (int g=0; g<list.Length; g++)
				{
					Generator grp2 = list[g];
					if (!(grp2 is Group)) continue;

					if (grp2.layout.field.Contains(grp.layout.field)) ArrayTools.Switch(list, grp, grp2);
				}
			}
		}

		#endregion


		public static GeneratorsAsset Default ()
		{
			GeneratorsAsset graph = ScriptableObject.CreateInstance<GeneratorsAsset>();

			//creating initial generators
			NoiseGenerator182 noiseGen = (NoiseGenerator182)graph.CreateGenerator(typeof(NoiseGenerator182), new Vector2(50,50));
			noiseGen.high = 0.9f;

			CurveGenerator curveGen = (CurveGenerator)graph.CreateGenerator(typeof(CurveGenerator), new Vector2(250,50));
			curveGen.curve = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,0,0), new Keyframe(1,1,2.5f,1) } );

			HeightOutput heightOut = (HeightOutput)graph.CreateGenerator(typeof(HeightOutput), new Vector2(450,50));

			curveGen.input.Link(noiseGen.output, noiseGen);
			heightOut.input.Link(curveGen.output, curveGen);

			return graph;
		}

		public static GeneratorsAsset DefaultVoxeland ()
		{
			GeneratorsAsset gens = ScriptableObject.CreateInstance<GeneratorsAsset>();

			//creating initial generators
			NoiseGenerator2 noiseGen = (NoiseGenerator2)gens.CreateGenerator(typeof(NoiseGenerator2), new Vector2(50,50));
			noiseGen.high = 0.9f;

			CurveGenerator curveGen = (CurveGenerator)gens.CreateGenerator(typeof(CurveGenerator), new Vector2(250,50));
			curveGen.curve = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,0,0), new Keyframe(1,1,2.5f,1) } );

			#if VOXELAND
			VoxelandOutput voxelandOut = (VoxelandOutput)gens.CreateGenerator(typeof(VoxelandOutput), new Vector2(450,50));
			ArrayTools.Add(ref voxelandOut.layers, createElement:() => new VoxelandOutput.Layer()); //voxelandOut.OnAddLayer(0,null);
			

			curveGen.input.Link(noiseGen.output, noiseGen);
			voxelandOut.layers[0].input.Link(curveGen.output, curveGen);
			#endif

			return gens;
		}

		/*#region Process/Apply/Purge caches

			public static Dictionary<Type, Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>>> processFunctionsCache = new Dictionary<Type, Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>>>();
			public static Dictionary<Type, Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator>> applyFunctionsCache = new Dictionary<Type, Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator>>();
			public static Dictionary<Type, Action<CoordRect, Terrain>> purgeFunctionsCache = new Dictionary<Type, Action<CoordRect, Terrain>>();

			public void FillProcessApplyCache ()
			{
				if (processFunctionsCache==null) processFunctionsCache = new Dictionary<Type, Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>>>();
				if (applyFunctionsCache==null) applyFunctionsCache = new Dictionary<Type, Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator>>();

				lock (processFunctionsCache)
				foreach (OutputGenerator outGen in GeneratorsOfType<OutputGenerator>(onlyEnabled:false, checkBiomes:true))
				{
					Type type = outGen.GetType();
					if (!processFunctionsCache.ContainsKey(type))
					{
						OutputGenerator o = outGen as OutputGenerator;
						processFunctionsCache.Add(type, o.GetProces());
						applyFunctionsCache.Add(type, o.GetApply());
					}
				}
			}

			public void FillPurgesCache ()
			{
				//using reflection since it will be used in editor only
				purgeFunctionsCache = new Dictionary<Type, Action<CoordRect, Terrain>>();

				foreach (Type type in typeof(OutputGenerator).Subtypes())
				{
					Action<CoordRect,Terrain> purgeFn = (Action<CoordRect,Terrain>)type.GetMethod("GetPurge", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null,null); 
					purgeFunctionsCache.Add(type, purgeFn);
				} 
			}

		#endregion*/

		#region Serialization


			public int serializedVersion = 0;
			
			public string[] classes = new string[0];
			public UnityEngine.Object[] objects = new UnityEngine.Object[0];
			public float[] floats = new float[0];

			public List<object> references = new List<object>();
			

			public bool setDirty;


			//outdated
			public Serializer serializer = new Serializer();
			public int listNum = 0;


			public void OnBeforeSerialize ()
			{ 
				//System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch(); timer.Start();
			
				serializedVersion = MapMagic.version;

				List<string> classesList = new List<string>();  
				List<UnityEngine.Object> objectsList = new List<UnityEngine.Object>();
				List<float> floatsList = new List<float>();
				references.Clear();

				CustomSerialization.WriteClass(list, classesList, objectsList, floatsList, references);

				classes = classesList.ToArray(); 
				objects = objectsList.ToArray();
				floats = floatsList.ToArray();

				//serializer.Clear();  
				//listNum = serializer.Store(list); 
				
				//timer.Stop(); Debug.Log("Serialize Time: " + timer.ElapsedMilliseconds + "ms");
			}

			public void OnAfterDeserialize ()
			{
				//System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch(); timer.Start(); 

				if (serializedVersion < 10) Debug.LogError("MapMagic: trying to load unknow version scene (v." + serializedVersion/10f + "). " +
					"This may cause errors or drastic drop in performance. " +  
					"Delete this MapMagic object and create the new one from scratch when possible."); 

				//loading old serializer
				if (classes.Length==0 && serializer.entities.Count!=0)
				{
					serializer.ClearLinks();
					list = (Generator[])serializer.Retrieve(listNum);
					serializer.ClearLinks();

					OnBeforeSerialize();
					serializer = null; //will not make it null, just 0-length
				}

				List<string> classesList = new List<string>();  classesList.AddRange(classes);
				List<UnityEngine.Object> objectsList = new List<UnityEngine.Object>();  objectsList.AddRange(objects);
				List<float> floatsList = new List<float>();  floatsList.AddRange(floats);

				references.Clear();
				list = (Generator[])CustomSerialization.ReadClass(0, classesList, objectsList, floatsList, references);
				references.Clear();

				//FillProcessApplyCache();
				//FillPurgesCache();

				//timer.Stop(); Debug.Log("Deserialize Time: " + timer.ElapsedMilliseconds + "ms");
			}


		#endregion



	}
}

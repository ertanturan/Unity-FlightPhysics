using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace MapMagic
{
	[System.Serializable]
	public class Serializer 
	{
		[System.Serializable] public struct BaseValue { public object val; public string name; public bool property; }
		[System.Serializable] public struct ObjectValue { public int link; public string name; public bool property; }
		
		[System.Serializable] public struct BoolValue { public bool val; public string name; public bool property; }
		[System.Serializable] public struct IntValue { public int val; public string name; public bool property; }
		[System.Serializable] public struct FloatValue { public float val; public string name; public bool property; }
		[System.Serializable] public struct StringValue { public string val; public string name; public bool property; }
		[System.Serializable] public struct CharValue { public char val; public string name; public bool property; }
		[System.Serializable] public struct FloatArray { public float[] val; public string name; public bool property; }
		[System.Serializable] public struct RectValue { public Rect val; public string name; public bool property; }
		[System.Serializable] public struct UnityObjValue { public UnityEngine.Object val; public string name; public bool property; }
	
		[System.Serializable]
		public class SerializedObject
		{
			public object obj; //to skip already added objs
			public string typeName;
			public List<ObjectValue> links = new List<ObjectValue>();

			public List<BoolValue> bools = new List<BoolValue>();
			public List<IntValue> ints = new List<IntValue>();
			public List<FloatValue> floats = new List<FloatValue>();
			public List<StringValue> strings = new List<StringValue>();
			public List<CharValue> chars = new List<CharValue>();
			public List<FloatArray> floatArrays = new List<FloatArray>();
			public List<RectValue> rects = new List<RectValue>();
			public List<UnityObjValue> unityObjs = new List<UnityObjValue>();

			public IList GetListByType (System.Type type)
			{
				if (type == typeof(bool)) return bools;
				if (type == typeof(int)) return ints;
				return links;
			}

			public void AddValue (System.Type type, object val, string name, Serializer ser) //serializer needed to store obj links
			{
				//if (val == null) links.Add( new ObjectValue() {link=-1, name=name} );
				if (type == typeof(bool)) bools.Add( new BoolValue() {val=(bool)val, name=name} );
				else if (type == typeof(int)) ints.Add( new IntValue() {val=(int)val, name=name} );
				else if (type == typeof(float)) floats.Add( new FloatValue() {val=(float)val, name=name} );
				else if (type == typeof(string)) strings.Add( new StringValue() {val=(string)val, name=name, property=false} );
				else if (type == typeof(char)) chars.Add( new CharValue() {val=(char)val, name=name, property=false} );
				else if (type == typeof(Rect)) rects.Add( new RectValue() {val=(Rect)val, name=name, property=false} );
				else if (type.IsSubclassOf(typeof(UnityEngine.Object))) unityObjs.Add( new UnityObjValue() {val=(UnityEngine.Object)val, name=name, property=false} );
				else if (type == typeof(float[])) floatArrays.Add( new FloatArray() {val=(float[])val, name=name, property=false} );
				
				else if (type == typeof(AnimationCurve))
				{
					int link = ser.Store(val);
					ser.entities[link].AddValue(typeof(Keyframe[]), ((AnimationCurve)val).keys, "keys", ser);
					links.Add( new ObjectValue() {link=ser.Store(val), name=name} );
				}
				else if (type == typeof(Keyframe))
				{
					int link = ser.Store(val);
					
					Keyframe tval = (Keyframe)val;
					ser.entities[link].AddValue(typeof(float), tval.time, "time", ser);
					ser.entities[link].AddValue(typeof(float), tval.value, "value", ser);
					ser.entities[link].AddValue(typeof(float), tval.inTangent, "inTangent", ser);
					ser.entities[link].AddValue(typeof(float), tval.outTangent, "outTangent", ser);

					links.Add( new ObjectValue() {link=ser.Store(val), name=name} );
				}
				else if (type == typeof(Matrix) ||
						 type == typeof(CoordRect) ||
						 type == typeof(Coord)) links.Add( new ObjectValue() {link=ser.Store(val, writeProperties:false), name=name} );


				//adding object
				else links.Add( new ObjectValue() {link=ser.Store(val), name=name} );
			}

			public void AddValues (System.Type type, Array array, Serializer ser) //serializer needed to store obj links
			{
				if (type == typeof(bool)) { for(int i=0;i<array.Length;i++) bools.Add( new BoolValue(){val=(bool)array.GetValue(i)} ); }
				else if (type == typeof(int)) { for(int i=0;i<array.Length;i++) ints.Add( new IntValue(){val=(int)array.GetValue(i)} ); }
				else if (type == typeof(float)) { for(int i=0;i<array.Length;i++) floats.Add( new FloatValue(){val=(float)array.GetValue(i)} ); }

				//adding everything else
				else 
				{
					for (int i=0;i<array.Length;i++) 
						AddValue(type, array.GetValue(i), "", ser); 
				}
			}

			public object GetValue (System.Type type, string name, Serializer ser)
			{
				if(type == typeof(bool))  { for(int i=0;i<bools.Count;i++)  if(bools[i].name==name)  return bools[i].val; }
				else if(type == typeof(int))  { for(int i=0;i<ints.Count;i++)  if(ints[i].name==name)  return ints[i].val; }
				else if(type == typeof(float))  { for(int i=0;i<floats.Count;i++)  if(floats[i].name==name)  return floats[i].val; }
				else if(type == typeof(string))  { for(int i=0;i<strings.Count;i++)  if(strings[i].name==name)  return strings[i].val; }
				else if(type == typeof(char))  { for(int i=0;i<chars.Count;i++)  if(chars[i].name==name)  return chars[i].val; }
				else if(type == typeof(Rect))  { for(int i=0;i<rects.Count;i++)  if(rects[i].name==name)  return rects[i].val; }
				else if(type.IsSubclassOf(typeof(UnityEngine.Object)))  
				{ 
					for(int i=0;i<unityObjs.Count;i++)  
						if(unityObjs[i].name==name)
						{
							//if (unityObjs[i].val == null) return null;   
							try { if (unityObjs[i].val.GetType() == typeof(UnityEngine.Object)) return null; }//else if (unityObjs[i].val.GetInstanceID() == 0) return null; 
							catch { return null; }

							return unityObjs[i].val;
						}   
				}
				else if (type == typeof(float[]))  { for(int i=0;i<floatArrays.Count;i++)  if(floatArrays[i].name==name)  return floatArrays[i].val; }

				//loading object
				else  { for(int i=0;i<links.Count;i++)  if(links[i].name==name)  return ser.Retrieve(links[i].link); }

				return null;
			}

			public Array GetValues (System.Type elementType, Serializer ser)
			{
				IList list = GetListByType(elementType);
				Array array = Array.CreateInstance(elementType, list.Count);

				if(elementType == typeof(bool))  { for(int i=0;i<bools.Count;i++) array.SetValue(bools[i].val, i); }
				else if(elementType == typeof(int))  { for(int i=0;i<ints.Count;i++) array.SetValue(ints[i].val, i); }
				else if(elementType == typeof(float))  { for(int i=0;i<floats.Count;i++) array.SetValue(floats[i].val, i); }
				else if(elementType == typeof(string))  { for(int i=0;i<strings.Count;i++) array.SetValue(strings[i].val, i); }
				else if(elementType == typeof(char))  { for(int i=0;i<chars.Count;i++) array.SetValue(chars[i].val, i); }
				else if(elementType == typeof(Rect))  { for(int i=0;i<rects.Count;i++) array.SetValue(rects[i].val, i); }
				else if(elementType.IsSubclassOf(typeof(UnityEngine.Object)))  { for(int i=0;i<unityObjs.Count;i++) array.SetValue(unityObjs[i].val, i); }
				else if (elementType == typeof(float[]))  { for(int i=0;i<floatArrays.Count;i++) array.SetValue(floatArrays[i].val, i); }

				else { for(int i=0;i<links.Count;i++) array.SetValue(ser.Retrieve(links[i].link), i); }

				return array;
			}

			public bool Equals (SerializedObject obj)
			{
				if (bools.Count != obj.bools.Count) return false; for (int i=bools.Count-1; i>=0; i--) if (bools[i].val!=obj.bools[i].val || bools[i].name!=obj.bools[i].name) return false;
				if (ints.Count != obj.ints.Count) return false; for (int i=ints.Count-1; i>=0; i--) if (ints[i].val!=obj.ints[i].val || ints[i].name!=obj.ints[i].name) return false;
				if (floats.Count != obj.floats.Count) return false; for (int i=floats.Count-1; i>=0; i--) if (floats[i].val!=obj.floats[i].val || floats[i].name!=obj.floats[i].name) return false;
				if (strings.Count != obj.strings.Count) return false; for (int i=strings.Count-1; i>=0; i--) if (strings[i].val!=obj.strings[i].val || strings[i].name!=obj.strings[i].name) return false;
				if (chars.Count != obj.chars.Count) return false; for (int i=chars.Count-1; i>=0; i--) if (chars[i].val!=obj.chars[i].val || chars[i].name!=obj.chars[i].name) return false;
				//if (rects.Count != obj.rects.Count) return false; for (int i=rects.Count-1; i>=0; i--) if (rects[i].val!=obj.rects[i].val || rects[i].name!=obj.rects[i].name) return false;
				if (unityObjs.Count != obj.unityObjs.Count) return false; for (int i=unityObjs.Count-1; i>=0; i--) if (unityObjs[i].val!=obj.unityObjs[i].val || unityObjs[i].name!=obj.unityObjs[i].name) return false;
				
				if (floatArrays.Count != obj.floatArrays.Count) return false; 
				for (int i=floatArrays.Count-1; i>=0; i--) 
				{
					if (floatArrays[i].name!=obj.floatArrays[i].name) return false;
					if (floatArrays[i].val.Length!=obj.floatArrays[i].val.Length) return false;
					for (int j=0; j<floatArrays[i].val.Length; j++)
						if (floatArrays[i].val[j]!=obj.floatArrays[i].val[j]) return false;
				}
				return true;
			}
		}

		public List<SerializedObject> entities = new List<SerializedObject>();


		public int Store (object obj, bool writeProperties=true) 
		{
			//storing nulls to -1
			if (obj == null) return -1;
			
			//if this object already added returning it's num
			int entitiesCount = entities.Count; 
			for (int i=0; i<entitiesCount; i++)
				if (obj == entities[i].obj) return i;
			
			//creating entity
			SerializedObject entity = new SerializedObject();
			System.Type objType = obj.GetType();
			entity.typeName = objType.AssemblyQualifiedName.ToString();
			entity.obj = obj;

			//adding entity to list before storing other objs
			entities.Add(entity);
			int result = entities.Count-1;

			//writing arrays
			if (objType.IsArray)
			{
				Array array = (Array)obj;
				System.Type elementType = objType.GetElementType();
				entity.AddValues(elementType, array, this);
				//for (int i=0;i<array.Length;i++) entity.AddValue(elementType, array.GetValue(i), "", this);
				return result;
			}

			//writing fields
			FieldInfo[] fields = objType.GetFields(BindingFlags.Public | BindingFlags.Instance); //BindingFlags.NonPublic - does not work in web player
			for (int i=0; i<fields.Length; i++)
			{
				FieldInfo field = fields[i];
				if (field.IsLiteral) continue; //leaving constant fields blank
				if (field.FieldType.IsPointer) continue; //skipping pointers (they make unity crash. Maybe require unsafe)
				if (field.IsNotSerialized) continue;

				entity.AddValue(field.FieldType, field.GetValue(obj), field.Name, this);
			}

			//writing properties
			if (writeProperties)
			{
				PropertyInfo[] properties = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
				for (int i=0;i<properties.Length;i++) 
				{
					PropertyInfo prop = properties[i];
					if (!prop.CanWrite) continue;
					if (prop.Name == "Item") continue; //ignoring this[x] 

					entity.AddValue(prop.PropertyType, prop.GetValue(obj,null), prop.Name, this);
				}
			}

			return result;
		}


		public object Retrieve (int num)
		{
			//checking if object is null
			if (num < 0) return null;
			
			//checking if this object was already retrieved
			if (entities[num].obj != null) return entities[num].obj;
			
			SerializedObject entity = entities[num];
			System.Type type = System.Type.GetType(entity.typeName);
			if (type == null) type = System.Type.GetType(entity.typeName.Substring(0, entity.typeName.IndexOf(","))); //trying to get type using it's short name
			if (type == null) return null; //in case this type do not exists anymore

			//retrieving arrays
			if (type.IsArray)
			{
				Array array = entity.GetValues(type.GetElementType(),this);
				entity.obj = array; 
				return array;
			}

			//creating instance
			object obj = System.Activator.CreateInstance(type);
			entity.obj = obj; //signing record.obj before calling Retrieve to avoid infinite loop

			//loading values
			FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
			for (int f=0; f<fields.Length; f++)
			{
				FieldInfo field = fields[f];
				if (field.IsLiteral) continue; //leaving constant fields blank
				if (field.FieldType.IsPointer) continue; //skipping pointers (they make unity crash. Maybe require unsafe)
				if (field.IsNotSerialized) continue;

				object val = null;
				try { val = entity.GetValue(field.FieldType, field.Name, this); }
				catch (System.Exception e) { Debug.LogError("Serialization error:\n" + e); }

				field.SetValue(obj, val);
			}

			//loading properties
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			for (int p=0; p<properties.Length; p++) 
			{
				PropertyInfo prop = properties[p];
				if (!prop.CanWrite) continue;
				if (prop.Name == "Item") continue; //ignoring this[x] 

				object val = null;
				try { val = entity.GetValue(prop.PropertyType, prop.Name, this); }
				catch (System.Exception e) { Debug.LogError("Serialization error:\n" + e); }
				
				if (val != null) 
					prop.SetValue(obj, val, null);
			}

			return obj;
		}

		public void ClearLinks () { for (int i=0; i<entities.Count; i++) entities[i].obj=null; } //use this both after all save and load to avoid remaining obj links
		public void Clear () { entities.Clear(); }

		public bool Equals (Serializer ser)
		{
			if (entities.Count != ser.entities.Count) return false;

			int count = entities.Count;
			for (int i=0; i<count; i++) 
				if (!entities[i].Equals(ser.entities[i])) 
					{ Debug.Log(entities[i].typeName); return false; }
					
			return true;
		}

		//a standalone function for the deep copy
		public static object DeepCopy (object obj)
		{
			Serializer serializer = new Serializer();
			
			serializer.Store(obj); 
			
			serializer.ClearLinks();
			object result = serializer.Retrieve(0);
			serializer.ClearLinks();

			return result;
		}
	}
}
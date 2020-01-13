using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;
using System;

namespace MapMagic
{

	public static class CustomSerialization 
	{
		public interface IStruct
		{
			string Encode ();
			void Decode (string[] lineMembers);
		}

		public interface IStructLink
		{
			string Encode (Func<object,int> writeClass);
			void Decode (string[] lineMembers, System.Func<int,object> readClass);
		}

		public static Type GetStandardAssembliesType (string s)
		{
			if (s.StartsWith("Plugins.")) s = s.Replace("Plugins", typeof(CustomSerialization).Namespace);
			Type type = Type.GetType(s);
			if (type == null) type = Type.GetType(s + ", UnityEngine");
			if (type == null) type = Type.GetType(s + ", Assembly-CSharp-Editor"); //trying to load from editor assembly
			if (type == null) type = Type.GetType(s + ", Assembly-CSharp"); //or from non-editor

			if (type == null) //lastly trying to load type from plugins
			{
				if (s.StartsWith("MapMagic")) s = s.Replace("MapMagic", "Plugins");
				if (s.StartsWith("Voxeland")) s = s.Replace("Voxeland", "Plugins");
				
				type = Type.GetType(s);
				if (type == null) type = Type.GetType(s + ", UnityEngine");
				if (type == null) type = Type.GetType(s + ", Assembly-CSharp-Editor"); //trying to load from editor assembly
				if (type == null) type = Type.GetType(s + ", Assembly-CSharp"); //or from non-editor
			} 

			return type;
		}

		public static bool IsNull (UnityEngine.Object uobj)
		{
			//comparing objects on serialization doesn't work in 2018.3.6: EnsureRunningOnMainThread can only be called from the main thread		
			#if UNITY_2018_3_OR_NEWER
			return (object)uobj == null;
			#else
			return uobj == (UnityEngine.Object)null;
			#endif
		}


		private struct Value 
		{
			public string name;
			public Type type;
			public object obj;
		}

		private static IEnumerable<Value> Values (object obj)
		{
			Type objType = obj.GetType();

			if (objType.IsArray)
			{
				Type elementType = objType.GetElementType();
				Array array = (Array)obj;

				//simple array 
				if (elementType.IsPrimitive) 
					yield return new Value() { name="items", type=objType, obj=array };

				//class array
		//		else if (elementType.IsClass && !elementType.IsValueType)
		//			yield return new Value() { name="links", type=objType, obj=array, classArray=true };
				
				//other arrays
				else
					for (int i=0; i<array.Length; i++) yield return new Value() { name="item"+i, type=elementType, obj=array.GetValue(i) };
			}

			else if (objType.IsSubclassOf(typeof(UnityEngine.Object)))
			{
				yield return new Value { name="Object", type=objType, obj=obj };
			}

			else
			{
				foreach (FieldInfo field in objType.UsableFields(includeStatic:true))
					yield return new Value { name=field.Name, type=field.FieldType, obj=field.GetValue(obj) };

				foreach (PropertyInfo prop in objType.UsableProperties())
					yield return new Value { name=prop.Name, type=prop.PropertyType, obj=prop.GetValue(obj,null) };
			}
		}


		public static int WriteClass (object obj, List<string> classes, List<UnityEngine.Object> objects, List<float> floats, List<object> references)
		{
			//cheking if this object was already saved
			//MapMagic.instance.gens.list[0].curve == MapMagic.instance.gens.list[1].curve  //false
			//MapMagic.instance.gens.list[0].curve.Equals(MapMagic.instance.gens.list[1].curve)  //true
			//that's why simple Contains won't work
			int refInd = references.IndexOf(obj);
			if (refInd>=0  &&  references[refInd]==obj) return refInd;

			//custom serialization
			if (obj is ISerializationCallbackReceiver)
				((ISerializationCallbackReceiver)obj).OnBeforeSerialize();

			//obj type
			System.Type objType = obj.GetType();
			System.Type elementType = objType.IsArray? objType.GetElementType() : null;
			string objTypeName = objType.ToString();

			//reserving a place in array
			int slotNum = classes.Count;
			classes.Add(null);

			//adding to references
			for (int i=references.Count; i<classes.Count; i++) references.Add(null); //references count should be equal to classes length
			references[slotNum] = obj;
		
			//writing
			var numFormat = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
			StringWriter writer = new StringWriter(numFormat);

			//header
			writer.Write("<" + objTypeName);
			if (objType.IsArray) writer.Write(" length=" + ((Array)obj).Length);
			writer.WriteLine(">");

			//values
			foreach (Value val in Values(obj))
			{
				if (val.type==typeof(float)) 
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " value=" + ((float)val.obj).ToString(numFormat) + "/>");

				else if (val.type==typeof(double)) 
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " value=" + ((double)val.obj).ToString(numFormat) + "/>");

				//primitive
				else if (val.type.IsPrimitive) 
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " value=" + val.obj + "/>");

				//null
				else if (val.obj == null) 
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " null/>");

				//string
				else if (val.type==typeof(string)) 
				{
					string str = (string)val.obj;
					str = str.Replace("\n", "\\n");
					str = str.Replace(" ", "\\_");
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " value=" + str + "/>"); //same as primitive, but after null
				}

				//custom struct
				else if (typeof(IStruct).IsAssignableFrom(val.type))
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " " + ((IStruct)val.obj).Encode() + "/>");

				//custom struct with links
				else if (typeof(IStructLink).IsAssignableFrom(val.type))
				{
					Func<object, int> getLinkFn = delegate(object linkObj) { return WriteClass(linkObj, classes, objects, floats, references); };
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " " + ((IStructLink)val.obj).Encode( getLinkFn ) + "/>");
				}

				//float array
				else if (objType == typeof(float[])) //note that obj should be used here, not val.obj
				{
					float[] array = (float[])obj;
					writer.WriteLine("\t<items type=" + val.type + " start=" + floats.Count + " length=" + array.Length + "/>");
					floats.AddRange(array);
				}

				//other primitive array
				else if (objType.IsArray && elementType.IsPrimitive)
				{
					writer.Write("\t<items type=" + val.type + " values=");

					Array array = (Array)obj;
					for (int i=0; i<array.Length; i++)
					{
						writer.Write(array.GetValue(i));
						if (i!=array.Length-1) writer.Write(',');
					}

					writer.WriteLine("/>");
				}

				//class array
				/*else if (val.classArray)
				{
					writer.Write( string.Format("\t<items type=" +  + " links=", val.type) );

					Array array = (Array)obj;
					for (int i=0; i<array.Length; i++)
					{
						writer.Write( WriteClass(val.obj, ref classes, ref objects, references) );
						if (i!=array.Length-1) writer.Write(',');
					}

					writer.Write("/>\n");
				}*///doesnt deal with null and not tested properly.

				//unity object
				else if (val.type.IsSubclassOf(typeof(UnityEngine.Object)))
				{
					//not a big deal if the object will be added to array twice
					writer.WriteLine("\t<" +  val.name + " type=" + val.type + " object=" + objects.Count + "/>");
					objects.Add((UnityEngine.Object)val.obj);
				}

				//null
				else if (val.obj == null)
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " link=-1/>");

				//class
				else if (val.type.IsClass && !val.type.IsValueType)
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " link=" + WriteClass(val.obj, classes, objects, floats, references) + "/>");

				//Vector2
				else if (val.type == typeof(Vector2))
				{
					Vector2 v2 = (Vector2)val.obj;
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " x=" + v2.x.ToString(numFormat) + " y=" + v2.y.ToString(numFormat) + "/>");
					//writer.WriteLine("\t<" + val.name + " type=" + val.type + " val=" + floats.Count + "/>");
					//floats.Add(v2.x); floats.Add(v2.y);
				}

				//Vector3
				else if (val.type == typeof(Vector3))
				{
					Vector3 v3 = (Vector3)val.obj;
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " x=" + v3.x.ToString(numFormat) + " y=" + v3.y.ToString(numFormat) + " z=" + v3.z.ToString(numFormat) + "/>");
					//writer.WriteLine("\t<" + val.name + " type=" + val.type + " val=" + floats.Count + "/>");
					//floats.Add(v3.x); floats.Add(v3.y); floats.Add(v3.z);
				}

				//Rect
				else if (val.type == typeof(Rect))
				{
					Rect rect = (Rect)val.obj;
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " x=" + rect.x.ToString(numFormat) + " y=" + rect.y.ToString(numFormat) + " width=" + rect.width.ToString(numFormat) + " height=" + rect.height.ToString(numFormat) + "/>");
					//writer.WriteLine("\t<" + val.name + " type=" + val.type + " val=" + floats.Count + "/>");
					//floats.Add(rect.x); floats.Add(rect.y); floats.Add(rect.width); floats.Add(rect.height);
				}

				//Color
				else if (val.type == typeof(Color))
				{
					Color c = (Color)val.obj;
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " r=" + c.r.ToString(numFormat) + " g=" + c.g.ToString(numFormat) + " b=" + c.b.ToString(numFormat) + " a=" + c.a.ToString(numFormat) + "/>");
				}

				//Vector4
				else if (val.type == typeof(Vector4))
				{
					Vector4 v4 = (Vector4)val.obj;
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " x=" + v4.x.ToString(numFormat) + " y=" + v4.y.ToString(numFormat) + " z=" + v4.z.ToString(numFormat) + " w=" + v4.w.ToString(numFormat) + "/>");
				}

				//Quaternion
				else if (val.type == typeof(Quaternion))
				{
					Quaternion q = (Quaternion)val.obj;
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " x=" + q.x.ToString(numFormat) + " y=" + q.y.ToString(numFormat) + " z=" + q.z.ToString(numFormat) + " w=" + q.w.ToString(numFormat) + "/>");
				}

				//enum
				else if (val.type.IsEnum)
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " value=" + (int)val.obj + "/>");

				//keyframe
				else if (val.type == typeof(Keyframe))
				{
					Keyframe k = (Keyframe)val.obj;
					#if UNITY_2018_2_OR_NEWER
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " time=" + k.time.ToString(numFormat) + " value=" + k.value.ToString(numFormat) + 
						" in=" + k.inTangent.ToString(numFormat) + " out=" + k.outTangent.ToString(numFormat) + 
						" mode=0" + //using free mode since tangent type is set by tangents themselves in 2018
						" inWeight=" + k.inWeight.ToString(numFormat) + " outWeight=" + k.outWeight.ToString(numFormat) + "/>");
					#else
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " time=" + k.time.ToString(numFormat) + " value=" + k.value.ToString(numFormat) + " in=" + k.inTangent.ToString(numFormat) + " out=" + k.outTangent.ToString(numFormat) + " mode=" + k.tangentMode + "/>");
					#endif
				}

				//any other struct (same as class)
				else
					writer.WriteLine("\t<" + val.name + " type=" + val.type + " link=" +  WriteClass(val.obj, classes, objects, floats, references) + "/>");
			}

			//footer
			writer.WriteLine("</" + objTypeName + ">");

			//writing obj
			writer.Close();
			classes[slotNum] = writer.ToString();

			return slotNum;
		}

		public static object ReadClass (int slotNum, List<string> classes, List<UnityEngine.Object> objects, List<float> floats, List<object> references)
		{
			//cheking if this object was already loaded
			for (int i=references.Count; i<classes.Count; i++) references.Add(null); //references count should be equal to classes length
			if (references[slotNum]!=null) return references[slotNum];

			object obj = null;

			StringReader reader = new StringReader(classes[slotNum]);

			//reading header
			string header = reader.ReadLine();
			header = header.Substring(1,header.Length-2);

			//getting the array length
			int arrayLength = 0;
			if (header.Contains(" length=")) 
			{
				string[] headerMembers = header.Split(' ');
				arrayLength = (int)headerMembers[1].Parse(typeof(int));
				header = headerMembers[0];
			}

			//finding object type
			System.Type objType = GetStandardAssembliesType(header);
			if (objType == null) { Debug.Log("Could not load " + header + " as this type does not exists anymore"); return null; }
			System.Type elementType = objType.IsArray? objType.GetElementType() : null;

			//creating object
			if (objType.IsArray) obj = Activator.CreateInstance(objType, arrayLength);
			else obj = Activator.CreateInstance(objType);
			references[slotNum] = obj;

			//reading values
			List<Value> values = new List<Value>();
			while (true)
			{
				string line = reader.ReadLine();
				if (line==null || line.StartsWith("</")) break;

				//if (line.StartsWith("\t")) line = line.Remove(0,1); //removing tab if any
				line = line.Substring(2,line.Length-4); //removing < and />
				string[] lineMembers = line.Split(' ', ',');

				Value val = new Value();
			
				//name
				val.name = lineMembers[0];
			
				//type
				string typeName = lineMembers[1].Remove(0,5); 
				val.type = GetStandardAssembliesType(typeName);
				if (val.type == null) { Debug.Log("Could not load " + typeName + " as this type does not exists anymore"); continue; } 

				//object: quick array
				if (val.type.IsArray && val.name=="items")
				{
					if (val.type == typeof(float[])) 
					{
						int start = (int)lineMembers[2].Parse(typeof(int));
						for (int i=start; i<start+arrayLength; i++) 
							values.Add( new Value() { name="item", type=elementType, obj=floats[i] } );
					}

					
					//class
					/*if (lineMembers[2].StartsWith("link"))
					{
						for (int i=2; i<lineMembers.Length; i++)
							values.Add( new Value() { name="item", type=elementType, obj=ReadClass( (int)lineMembers[i].Parse(typeof(int)), classes, objects, floats, references) } );
					}*/

					//primitives
					else
					{
						for (int i=2; i<lineMembers.Length; i++)
							values.Add( new Value() { name="item", type=elementType, obj=lineMembers[i].Parse(elementType) } );
					}
				}

				//object: other
				else
				{
					//null
					if (lineMembers[2]=="null") val.obj = null;

					//custom struct
					else if (typeof(IStruct).IsAssignableFrom(val.type))
					{
						val.obj = Activator.CreateInstance(val.type);
						((IStruct)val.obj).Decode(lineMembers);
					}

					//custom struct with links
					else if (typeof(IStructLink).IsAssignableFrom(val.type))
					{
						Func<int, object> readClassFn = delegate(int link) { return ReadClass(link, classes, objects, floats, references); };
						val.obj = Activator.CreateInstance(val.type);
						((IStructLink)val.obj).Decode(lineMembers, readClassFn);
					}
				
					//class
					else if (lineMembers[2].StartsWith("link")) 
					{
						//val.obj = int.Parse(lineMembers[2].Remove(0,5)); //postporning reading the class until the object is created, storing link
						val.obj = ReadClass(int.Parse(lineMembers[2].Remove(0,5)), classes, objects, floats, references);
					}

					//primitive
					else if (val.type.IsPrimitive)
						val.obj = lineMembers[2].Parse(val.type);

					//unity Object
					else if (val.type.IsSubclassOf(typeof(UnityEngine.Object)))
					{
						UnityEngine.Object uobj = objects[ (int)lineMembers[2].Parse(typeof(int)) ];

						if (!IsNull(uobj)) val.obj = uobj;
						else val.obj = null;
					}

					//string
					else if (val.type==typeof(string))
					{
						string str = (string)lineMembers[2].Parse(val.type);
						str = str.Replace("\\n", "\n");
						str = str.Replace("\\_", " ");
						val.obj = str;
					}

					//Vector2
					else if (val.type == typeof(Vector2))
						val.obj = new Vector2( (float)lineMembers[2].Parse(typeof(float)), (float)lineMembers[3].Parse(typeof(float)) );
					//{
					//	int n = (int)lineMembers[2].Parse(typeof(int));
					//	val.obj = new Vector2(floats[n], floats[n+1]);
					//}

					//Vector3
					else if (val.type == typeof(Vector3))
						val.obj = new Vector3( (float)lineMembers[2].Parse(typeof(float)), (float)lineMembers[3].Parse(typeof(float)), (float)lineMembers[4].Parse(typeof(float)) );
					//{
					//	int n = (int)lineMembers[2].Parse(typeof(int));
					//	val.obj = new Vector3(floats[n], floats[n+1], floats[n+2]);
					//}

					//Rect
					else if (val.type == typeof(Rect))
						val.obj = new Rect( (float)lineMembers[2].Parse(typeof(float)), (float)lineMembers[3].Parse(typeof(float)), (float)lineMembers[4].Parse(typeof(float)), (float)lineMembers[5].Parse(typeof(float)) );
					//{
					//	int n = (int)lineMembers[2].Parse(typeof(int));
					//	val.obj = new Rect(floats[n], floats[n+1], floats[n+2], floats[n+3]);
					//}

					//Color
					else if (val.type == typeof(Color))
						val.obj = new Color( (float)lineMembers[2].Parse(typeof(float)), (float)lineMembers[3].Parse(typeof(float)), (float)lineMembers[4].Parse(typeof(float)), (float)lineMembers[5].Parse(typeof(float)) );
				
					//Vector4
					else if (val.type == typeof(Vector4))
						val.obj = new Vector4( (float)lineMembers[2].Parse(typeof(float)), (float)lineMembers[3].Parse(typeof(float)), (float)lineMembers[4].Parse(typeof(float)), (float)lineMembers[5].Parse(typeof(float)) );
				
					//Quaternion
					else if (val.type == typeof(Quaternion))
						val.obj = new Quaternion( (float)lineMembers[2].Parse(typeof(float)), (float)lineMembers[3].Parse(typeof(float)), (float)lineMembers[4].Parse(typeof(float)), (float)lineMembers[5].Parse(typeof(float)) );
					
					//Keyframe
					else if (val.type == typeof(Keyframe))
					{
						Keyframe k = new Keyframe( (float)lineMembers[2].Parse(typeof(float)), (float)lineMembers[3].Parse(typeof(float)), (float)lineMembers[4].Parse(typeof(float)), (float)lineMembers[5].Parse(typeof(float)) );

						#if UNITY_2018_2_OR_NEWER
						if (lineMembers.Length > 7)
						{
							k.inWeight = (float)lineMembers[7].Parse(typeof(float));
							k.outWeight = (float)lineMembers[8].Parse(typeof(float));
						}
						else
						{
							if (lineMembers.Length >= 7)
								k.tangentMode = (int)lineMembers[6].Parse(typeof(int));
							else 
								k.tangentMode = 0;
						}
						#else
							if (lineMembers.Length >= 7)
								k.tangentMode = (int)lineMembers[6].Parse(typeof(int));
							else 
								k.tangentMode = 0;
						#endif

						val.obj = k;
					}
				
					//enum
					else if (val.type.IsEnum)
						val.obj = Enum.ToObject(val.type, (int)lineMembers[2].Parse(typeof(int)));

					values.Add(val);
				}
			}

			//filling object
			int valuesCount = values.Count;
			if (objType.IsArray)
			{
				Array array = (Array)obj;
				for (int i=0; i<array.Length; i++) array.SetValue(values[i].obj, i);
			}
			else 
			{
				//TODO: inline code here
				//TODO: use attributes
				foreach (FieldInfo field in objType.UsableFields(includeStatic:true))  
				{
					string fieldName = field.Name;
					Type fieldType = field.FieldType;

					for (int i=0; i<valuesCount; i++)
						try {
						if (values[i].name == fieldName  &&  values[i].type == fieldType) field.SetValue(obj, values[i].obj); }
						catch {}
				}
				foreach (PropertyInfo prop in objType.UsableProperties())
				{
					string propName = prop.Name;
					Type propType = prop.PropertyType;

					for (int i=0; i<valuesCount; i++)
						if (values[i].name == propName  &&  values[i].type == propType)
						{
							if (values[i].obj != null  &&  !values[i].obj.Equals(null))
								prop.SetValue(obj, values[i].obj, null);
						}
				}
			}

			//custom serialization
			if (obj is ISerializationCallbackReceiver)
				((ISerializationCallbackReceiver)obj).OnAfterDeserialize(); 

			return obj;
		}


		public static object DeepCopy (object src)
		{
			List<string> classes = new List<string>();
			List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
			List<float> floats = new List<float>();
			List<object> saveReferences = new List<object>();
			List<object> loadReferences = new List<object>();

			int num = CustomSerialization.WriteClass(src, classes, objects, floats, saveReferences);
			return CustomSerialization.ReadClass(num, classes, objects, floats, loadReferences);
		}
	
		public static string ExportXML (List<string> classes, List<UnityEngine.Object> objects, List<float> floats)
		{
			StringWriter writer = new StringWriter();

			for (int i=0; i<classes.Count; i++)
				writer.Write(classes[i]);

			#if UNITY_EDITOR
			for (int i=0; i<objects.Count; i++)
			{
				string path = UnityEditor.AssetDatabase.GetAssetPath(objects[i]);
				path = path.Replace(" ","*");
				writer.WriteLine("<Object type=" + objects[i].GetType() + " path=" + path + "/>");
			}
			#endif

			writer.Write("<Floats values=");
			int floatsCount = floats.Count;
			for (int i=0; i<floatsCount; i++) 
			{
				 writer.Write(floats[i].ToString());
				 if (i != floatsCount-1) writer.Write(",");
			}
			writer.WriteLine("/>");

			writer.Close();
			return writer.ToString();
		}

		public static void ImportXML (string xml, out List<string> classes, out List<UnityEngine.Object> objects, out List<float> floats)
		{
			StringReader reader = new StringReader(xml);
			
			classes = new List<string>();
			objects = new List<UnityEngine.Object>();
			floats = new List<float>();

			StringWriter writer = null;
			while (true)
			{
				string line = reader.ReadLine();
				if (line==null) break;

				//objects
				if (line.StartsWith("<Object")) 
				{
					#if UNITY_EDITOR
					line = line.Replace("/>","");
					string[] lineMemebers = line.Split(' ');

					string path = lineMemebers[2].Replace("path=","");
					path = path.Replace("*", " ");
					objects.Add(UnityEditor.AssetDatabase.LoadMainAssetAtPath(path));

					Type type = GetStandardAssembliesType( lineMemebers[1].Replace("type=","") );
					if (type==typeof(Transform) && objects[objects.Count-1]!=null) objects[objects.Count-1] = ((GameObject)objects[objects.Count-1]).transform; //converting to transform instead of gameobject
					#endif

					continue;
				}

				//floats
				if (line.StartsWith("<Floats")) 
				{
					line = line.Replace("<Floats values=","");
					line = line.Replace("/>","");
					if (line.Length==0) continue;

					string[] lineMemebers = line.Split(',');
					for (int i=0; i<lineMemebers.Length; i++) floats.Add(float.Parse(lineMemebers[i]));

					continue;
				}

				//classes
				if (!line.Contains("/>") && !line.Contains("</")) //class started
				{
					if (writer!=null) classes.Add(writer.ToString());
					writer = new StringWriter();
				}

				writer.WriteLine(line);
			}
			classes.Add(writer.ToString()); //writing down what's left in writer
		}
	}
}

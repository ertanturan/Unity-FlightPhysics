using System;
using System.Collections.Generic;

namespace MapMagic
{
	//surprisingly Unity's ArrayUtility is an Editor calss
	//so this is it's analog to use in builds
	
	public static class ArrayTools 
	{
		#region Array

			public static int Find(this Array array, object obj)
			{
				for (int i=0; i<array.Length; i++)
					if (array.GetValue(i) == obj) return i;
				return -1;
			}

			static public int Find<T> (T[] array, T obj) where T : class
			{
				for (int i=0; i<array.Length; i++)
					if (Equals(array[i],obj)) return i;
				return -1;
			}

			static public int FindEquatable<T> (T[] array, T obj) where T : IEquatable<T>
			{
				for (int i=0; i<array.Length; i++)
					if (Equals(array[i],obj)) return i;
				return -1;
			}
			

			static public void RemoveAt<T> (ref T[] array, int num) { array = RemoveAt(array, num); }
			static public T[] RemoveAt<T> (T[] array, int num)
			{
				T[] newArray = new T[array.Length-1];
				for (int i=0; i<newArray.Length; i++) 
				{
					if (i<num) newArray[i] = array[i];
					else newArray[i] = array[i+1];
				}
				return newArray;
			}

			static public void Remove<T> (ref T[] array, T obj) where T : class  {array = Remove(array, obj); }
			static public T[] Remove<T> (T[] array, T obj) where T : class
			{
				int num = Find<T>(array, obj);
				return RemoveAt<T>(array,num);
			}

			static public void Add<T> (ref T[] array, Func<T> createElement=null) { array = Add(array, createElement:createElement); }
			static public T[] Add<T> (T[] array, Func<T> createElement=null)
			{
				if (array==null || array.Length==0) 
				{ 
					if (createElement != null) return new T[] {createElement()};
					else return new T[] {default(T)};
				}

				T[] newArray = new T[array.Length+1];
				for (int i=0; i<array.Length; i++) 
					newArray[i] = array[i];
				
				if (createElement != null) newArray[array.Length] = createElement();
				else newArray[array.Length] = default(T);
				
				return newArray;
			}

			static public void Insert<T> (ref T[] array, int pos, Func<T> createElement=null) { array = Insert(array, pos, createElement:createElement); }
			static public T[] Insert<T> (T[] array, int pos, Func<T> createElement=null)
			{
				if (array==null || array.Length==0) 
				{ 
					if (createElement != null) return new T[] {createElement()};
					else return new T[] {default(T)};
				}
				if (pos > array.Length || pos < 0) pos = array.Length;
				
				T[] newArray = new T[array.Length+1];
				for (int i=0; i<newArray.Length; i++) 
				{
					if (i<pos) newArray[i] = array[i];
					else if (i == pos) 
					{
						if (createElement != null) newArray[i] = createElement();
						else newArray[i] = default(T);
					}
					else newArray[i] = array[i-1];
				}
				return newArray;
			}

			static public T[] InsertRange<T> (T[] array, int after, T[] add)
			{
				if (array==null || array.Length==0) { return add; }
				if (after > array.Length || after<0) after = array.Length;
				
				T[] newArray = new T[array.Length+add.Length];
				for (int i=0; i<newArray.Length; i++) 
				{
					if (i<after) newArray[i] = array[i];
					else if (i == after) 
					{
						for (int j=0; j<add.Length; j++)
							newArray[i+j] = add[j];
						i+= add.Length-1;
					}
					else newArray[i] = array[i-add.Length];
				}
				return newArray;
			}

			static public void Resize<T> (ref T[] array, int newSize, Func<int,T> createElementCallback=null) { array = Resize(array, newSize, createElementCallback); }
			static public T[] Resize<T> (T[] array, int newSize, Func<int,T> createElementCallback=null)
			{
				if (array.Length == newSize) return array;

				T[] newArray = new T[newSize];
					
				int min = newSize<array.Length? newSize : array.Length;
				for (int i=0; i<min; i++)
					newArray[i] = array[i];

				if (newSize > array.Length && createElementCallback!=null)
				{
					for (int i=array.Length; i<newSize; i++)
						newArray[i] = createElementCallback(i);
				}

				return newArray;
			}

			static public void Append<T> (ref T[] array, T[] additional) { array = Append(array, additional); }
			static public T[] Append<T> (T[] array, T[] additional)
			{
				T[] newArray = new T[array.Length+additional.Length];
				for (int i=0; i<array.Length; i++) { newArray[i] = array[i]; }
				for (int i=0; i<additional.Length; i++) { newArray[i+array.Length] = additional[i]; }
				return newArray;
			}

			static public void Switch<T> (T[] array, int num1, int num2)
			{
				if (num1<0 || num1>=array.Length || num2<0 || num2 >=array.Length) return;
				
				T temp = array[num1];
				array[num1] = array[num2];
				array[num2] = temp;
			}

			static public void Switch<T> (T[] array, T obj1, T obj2) where T : class
			{
				int num1 = Find<T>(array, obj1);
				int num2 = Find<T>(array, obj2);
				Switch<T>(array, num1, num2);
			}

			static public T[] Truncated<T> (this T[] src, int length)
			{
				T[] dst = new T[length];
				for (int i=0; i<length; i++) dst[i] = src[i];
				return dst;
			}

			public static bool Equals<T> (T[] a1, T[] a2) where T : class
			{
				if (a1.Length != a2.Length) return false;
				for (int i=0; i<a1.Length; i++)
					if (a1[i] != a2[i]) return false;
				return true;
			}

			public static bool EqualsEquatable<T> (T[] a1, T[] a2) where T : IEquatable<T>
			{
				if (a1.Length != a2.Length) return false;
				for (int i=0; i<a1.Length; i++)
					if (!Equals(a1[i],a2[i])) return false;
				return true;
			}

			public static bool EqualsVector3 (UnityEngine.Vector3[] a1, UnityEngine.Vector3[] a2, float delta=float.Epsilon)
			{
				if (a1==null || a2==null || a1.Length != a2.Length) return false;
				for (int i=0; i<a1.Length; i++)
				{
					float dist = a1[i].x-a2[i].x;
					if (!(dist<delta && -dist<delta)) return false;

					dist = a1[i].y-a2[i].y;
					if (!(dist<delta && -dist<delta)) return false;

					dist = a1[i].z-a2[i].z;
					if (!(dist<delta && -dist<delta)) return false;
				}
				return true;
			}

		#endregion

		#region Array Sorting

			static public void QSort (float[] array) { QSort(array, 0, array.Length-1); }
			static public void QSort (float[] array, int l, int r)
			{
				float mid = array[l + (r-l) / 2]; //(l+r)/2
				int i = l;
				int j = r;
				
				while (i <= j)
				{
					while (array[i] < mid) i++;
					while (array[j] > mid) j--;
					if (i <= j)
					{
						float temp = array[i];
						array[i] = array[j];
						array[j] = temp;
						
						i++; j--;
					}
				}
				if (i < r) QSort(array, i, r);
				if (l < j) QSort(array, l, j);
			}

			static public void QSort<T> (T[] array, float[] reference) { QSort(array, reference, 0, reference.Length-1); }
			static public void QSort<T> (T[] array, float[] reference, int l, int r)
			{
				float mid = reference[l + (r-l) / 2]; //(l+r)/2
				int i = l;
				int j = r;
				
				while (i <= j)
				{
					while (reference[i] < mid) i++;
					while (reference[j] > mid) j--;
					if (i <= j)
					{
						float temp = reference[i];
						reference[i] = reference[j];
						reference[j] = temp;

						T tempT = array[i];
						array[i] = array[j];
						array[j] = tempT;
						
						i++; j--;
					}
				}
				if (i < r) QSort(array, reference, i, r);
				if (l < j) QSort(array, reference, l, j);
			}

			static public void QSort<T> (List<T> list, float[] reference) { QSort(list, reference, 0, reference.Length-1); }
			static public void QSort<T> (List<T> list, float[] reference, int l, int r)
			{
				float mid = reference[l + (r-l) / 2]; //(l+r)/2
				int i = l;
				int j = r;
				
				while (i <= j)
				{
					while (reference[i] < mid) i++;
					while (reference[j] > mid) j--;
					if (i <= j)
					{
						float temp = reference[i];
						reference[i] = reference[j];
						reference[j] = temp;

						T tempT = list[i];
						list[i] = list[j];
						list[j] = tempT;
						
						i++; j--;
					}
				}
				if (i < r) QSort(list, reference, i, r);
				if (l < j) QSort(list, reference, l, j);
			}

			static public int[] Order (int[] array, int[] order=null, int max=0, int steps=1000000, int[] stepsArray=null) //returns an order int array
			{
				if (max==0) max=array.Length;
				if (stepsArray==null) stepsArray = new int[steps+1];
				else steps = stepsArray.Length-1;
			
				//creating starts array
				int[] starts = new int[steps+1];
				for (int i=0; i<max; i++) starts[ array[i] ]++;
					
				//making starts absolute
				int prev = 0;
				for (int i=0; i<starts.Length; i++)
					{ starts[i] += prev; prev = starts[i]; }

				//shifting starts
				for (int i=starts.Length-1; i>0; i--)
					{ starts[i] = starts[i-1]; }  
				starts[0] = 0;

				//using magic to compile order
				if (order==null) order = new int[max];
				for (int i=0; i<max; i++)
				{
					int h = array[i]; //aka height
					int num = starts[h];
					order[num] = i;
					starts[h]++;
				}
				return order;
			}

			static public T[] Convert<T,Y> (Y[] src)
			{
				T[] result = new T[src.Length];
				for (int i=0; i<src.Length; i++) result[i] = (T)(object)(src[i]);
				return result;
			}

		#endregion
	}

}

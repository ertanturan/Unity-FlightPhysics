using UnityEngine;
using System.Collections;
using UnityEditor;

namespace MapMagic
{

		public class PopupMenu : UnityEditor.PopupWindowContent
		{
			public class MenuItem 
			{
				public string name;
				public bool clicked;
				public bool disabled;
				public int priority;
				public MenuItem[] subItems = null;
			
				public System.Action onClick; //action called when subitem selected

				public int Count { get{ return subItems==null ? 0 : subItems.Length; } }
				public bool hasSubs { get{ return subItems!=null;} }

				public MenuItem (string name) { this.name=name; }
				public MenuItem (string name, System.Action onClick, bool disabled=false, int priority=0) { this.name=name; this.onClick=onClick; this.disabled=disabled; this.priority=priority; }
				public MenuItem (string name, MenuItem[] subs, bool disabled=false, int priority=0) { this.name=name; subItems=subs; this.disabled=disabled; this.priority=priority; }
				public MenuItem () { }

				public void SortItems ()
				{
					if (subItems==null) return;
					for (int i=0; i<subItems.Length; i++) 
						for (int j=0; j<subItems.Length; j++) 
					{
						if (subItems[i].priority == subItems[j].priority) //sorting by name if priority equal
						{ 
							for (int c=0; c<Mathf.Min(subItems[i].name.Length, subItems[j].name.Length); c++)
								if ((int)(subItems[i].name[c]) != (int)(subItems[j].name[c]))
								{
									if ((int)(subItems[i].name[c]) < (int)(subItems[j].name[c])) ArrayTools.Switch(subItems, subItems[i], subItems[j]);
									break;
								}
						}

						if (subItems[i].priority < subItems[j].priority) ArrayTools.Switch(subItems, subItems[i], subItems[j]);
					}
				
					for (int i=0; i<subItems.Length; i++) subItems[i].SortItems();
				}
			}

			public int lineHeight = 20;
			public int width = 120;

			static GUIStyle blackLabel;

			static private Texture2D background;
			static private Texture2D highlight;

			public MenuItem[] items;

			private MenuItem lastItem;
			private System.DateTime lastTimestart;
			//private bool timeUsed = false;

			private MenuItem expandedItem;

			private PopupMenu parent;

			private PopupMenu expandedWindow = null;
		
			//void CloseMenuIfNotFocused () { if (UnityEditor.EditorWindow.focusedWindow.GetType() != typeof(PopupMenu)) this.Close(); } 
			//void OnEnable () { UnityEditor.EditorApplication.update += CloseMenuIfNotFocused; }
			//void OnDisable () { UnityEditor.EditorApplication.update -= CloseMenuIfNotFocused; }

			public static Texture2D triangle;

			static public PopupMenu DrawPopup (MenuItem[] items, Vector2 pos, bool closeAllOther=false, bool sort=true)
			{
				//sorting items according to their priority/name
				if (sort)
				{
					for (int i=0; i<items.Length; i++) 
						for (int j=0; j<items.Length; j++) 
						{
							if (items[i].priority == items[j].priority) //sorting by name if priority equal
							{ 
								if ((int)(items[i].name[0]) < (int)(items[j].name[0])) ArrayTools.Switch(items, items[i], items[j]);
							}
							else if (items[i].priority < items[j].priority) ArrayTools.Switch(items, items[i], items[j]);
						}
					for (int i=0; i<items.Length; i++) items[i].SortItems();
				}
								
				PopupMenu popupWindow = new PopupMenu();
				popupWindow.items = items;
				PopupWindow.Show(new Rect(pos.x,pos.y,0,0), popupWindow);
				return popupWindow;
			}

			public override Vector2 GetWindowSize() {return new Vector2(width, lineHeight*items.Length+4);}

			public override void OnGUI(Rect rect)
			{
				if (background==null)
				{
					background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
					background.SetPixel(0, 0, new Color(0.98f, 0.98f, 0.98f));
					background.Apply();
				}
			
				if (highlight==null)
				{
					highlight = new Texture2D(1, 1, TextureFormat.RGBA32, false);
					highlight.SetPixel(0, 0, new Color(0.6f, 0.7f, 0.9f));
					highlight.Apply();
				}

				//background
				//if (Event.current.type == EventType.repaint) GUI.skin.box.Draw(fullRect, false, true, true, false);
				GUI.DrawTexture(new Rect(1, 1, width-2, lineHeight*items.Length), background, ScaleMode.StretchToFill);

				//list
				for (int i=0; i<items.Length; i++)
				{
					MenuItem currentItem = items[i];
					
					currentItem.clicked = false;
					Rect lineRect = new Rect(1, i*lineHeight+1, width-2, lineHeight-2);
					bool highlighted = lineRect.Contains(Event.current.mousePosition);
					if (currentItem.disabled) highlighted = false;

					if (highlighted) GUI.DrawTexture(lineRect, highlight);
					
					//labels
					UnityEditor.EditorGUI.BeginDisabledGroup(currentItem.disabled);
					if (blackLabel == null) { blackLabel = new GUIStyle(UnityEditor.EditorStyles.label); blackLabel.normal.textColor = Color.black; }
					UnityEditor.EditorGUI.LabelField(new Rect(lineRect.x+3, lineRect.y, lineRect.width-3, lineRect.height), currentItem.name, blackLabel);
					UnityEditor.EditorGUI.EndDisabledGroup();

					if (currentItem.hasSubs)
					{
						Rect rightRect = lineRect; rightRect.width = 10; rightRect.height = 10; 
							rightRect.x = lineRect.x + lineRect.width - rightRect.width; rightRect.y = lineRect.y + lineRect.height/2 - rightRect.height/2;
						//UnityEditor.EditorGUI.LabelField(rightRect, "\u25B6");
						if (triangle == null) triangle = Resources.Load("PopupMenuTrangleIcon") as Texture2D; 
						GUI.DrawTexture(rightRect, triangle, ScaleMode.ScaleAndCrop);
					}

					//opening subsmenus
					if (highlighted)
					{
						bool clicked = Event.current.type == EventType.MouseDown && Event.current.type == 0;

						if (clicked && currentItem.onClick != null)
						{
							currentItem.onClick();
							CloseRecursive();
						}
						
						//starting timer on selected item change
						if (currentItem != lastItem)
						{
							lastTimestart = System.DateTime.Now;
							lastItem = currentItem;
						}

						//when holding for too long
						double highlightTime = (System.DateTime.Now-lastTimestart).TotalMilliseconds;
						if ((highlightTime > 300 && expandedItem != currentItem) || clicked) 
						{
							//re-opening expanded window
							if (expandedWindow != null && expandedWindow.editorWindow != null) expandedWindow.editorWindow.Close();
							expandedWindow = new PopupMenu() { items=currentItem.subItems, parent=this };
							expandedItem = currentItem;
							if (currentItem.subItems != null) PopupWindow.Show(new Rect(lineRect.max-new Vector2(0,lineHeight), Vector2.zero), expandedWindow);
						}
					}
				}

				#if (!UNITY_EDITOR_LINUX)
				this.editorWindow.Repaint();
				#endif
			}

			public override void OnClose () { if (expandedWindow != null && expandedWindow.editorWindow != null) expandedWindow.editorWindow.Close(); }

			public void CloseRecursive ()
			{
				if (parent != null) parent.CloseRecursive();
				editorWindow.Close();
			}
		}

}
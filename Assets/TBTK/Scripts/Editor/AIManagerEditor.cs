using UnityEngine;
using UnityEditor;

using System;

using System.Collections;
using System.Collections.Generic;

using TBTK;

namespace TBTK{

	[CustomEditor(typeof(AIManager))]
	public class AIManagerEditor : Editor {

		private static AIManager instance;
		
		private static bool showDefaultFlag=false;
		
		
		private static string[] AIModeLabel=new string[0];
		private static string[] AIModeTooltip=new string[0];
		
		
		private GUIContent cont;
		private GUIContent[] contList;
		
		
		void Awake(){
			instance = (AIManager)target;
			
			int enumLength = Enum.GetValues(typeof(_AIMode)).Length;
			AIModeLabel=new string[enumLength];
			AIModeTooltip=new string[enumLength];
			for(int i=0; i<enumLength; i++){
				AIModeLabel[i]=((_AIMode)i).ToString();
				if((_AIMode)i==_AIMode.Passive) AIModeTooltip[i]="the unit wont move unless the there are hostile within the faction's sight (using unit sight value even when Fog-Of-War is not used)";
				else if((_AIMode)i==_AIMode.Trigger) AIModeTooltip[i]="the unit wont move unless it's being triggered, when it spotted any hostile or attacked";
				else if((_AIMode)i==_AIMode.Aggressive) AIModeTooltip[i]="the unit will be on move all the time, looking for potential target";
			}
			
			EditorUtility.SetDirty(instance);
		}
		
		
		
		
		public override void OnInspectorGUI(){
			
			GUI.changed = false;
			
			EditorGUILayout.Space();
			
			
			int aiMode=(int)instance.mode;
			cont=new GUIContent("AI Mode:", "The default AI mode to be used by all faction, if not assigned to other mode");
			contList=new GUIContent[AIModeLabel.Length];
			for(int i=0; i<contList.Length; i++) contList[i]=new GUIContent(AIModeLabel[i], AIModeTooltip[i]);
			aiMode = EditorGUILayout.Popup(cont, aiMode, contList);
			instance.mode=(_AIMode)aiMode;
			
			
			cont=new GUIContent("MoveUntriggeredUnit:", "Check to enable untriggered unit to move randomly (without actively pursuing any hostile)");
			instance.untriggeredUnitMove=EditorGUILayout.Toggle(cont, instance.untriggeredUnitMove);
			
			
			EditorGUILayout.Space();
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("", GUILayout.MaxWidth(10));
			showDefaultFlag=EditorGUILayout.Foldout(showDefaultFlag, "Show default editor");
			EditorGUILayout.EndHorizontal();
			if(showDefaultFlag) DrawDefaultInspector();
			
			
			if(GUI.changed) EditorUtility.SetDirty(instance);
			
		}
		
		
		
	}

	
}
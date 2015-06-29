using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using TBTK;

namespace TBTK{
	
	//this is to show indicator for player unit that have finish their move
	//it only works on FactionPerTurn and Free move order
	public class MovedUnitIndicator : MonoBehaviour {
		
		public Transform indicatorPrefab;
		public int maxUnitCount=10;
		private List<Transform> indicatorList=new List<Transform>();
		
		// Use this for initialization
		void Start () {
			if(TurnControl.GetTurnMode()!=_TurnMode.FactionPerTurn || TurnControl.GetMoveOrder()!=_MoveOrder.Free){
				this.enabled=false;
				gameObject.SetActive(false);
				return;
			}
			
			//create the indicator
			for(int i=0; i<maxUnitCount; i++){
				Transform indT=(Transform)Instantiate(indicatorPrefab);
				indT.parent=transform;
				indicatorList.Add(indT);
			}
			OnNewTurn();
		}
		
		void OnEnable(){
			GameControl.onIterateTurnE += OnNewTurn;
			Unit.onMoveDepletedE += OnUnitMoveDepleted;
		}
		void OnDisable(){
			GameControl.onIterateTurnE -= OnNewTurn;
			Unit.onMoveDepletedE -= OnUnitMoveDepleted;
		}
		
		//new turn, clear all indicator
		void OnNewTurn(){
			for(int i=0; i<indicatorList.Count; i++){
				indicatorList[i].gameObject.SetActive(false);
			}
		}
		
		//when a unit completes it's move, put an unused indicator on the unit tile
		//this will only be called if the unit in question is player unit
		void OnUnitMoveDepleted(Unit unit){
			for(int i=0; i<indicatorList.Count; i++){
				if(indicatorList[i].gameObject.activeInHierarchy) continue;
				
				indicatorList[i].position=unit.tile.GetPos()+new Vector3(0, 0.05f, 0);
				indicatorList[i].gameObject.SetActive(true);
				
				break;
			}
		}
		
	}

}
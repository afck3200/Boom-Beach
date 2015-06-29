﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using TBTK;

namespace TBTK {

	public class PerkManager : MonoBehaviour {
		
		public delegate void UpdatePerkPointHandler(int point);
		public static event UpdatePerkPointHandler onPerkPointE;
		
		public delegate void UpdatePerkCurrencyHandler(int point);
		public static event UpdatePerkCurrencyHandler onPerkCurrencyE;
		
		public delegate void PerkPurchasedHandler(Perk perk);
		public static event PerkPurchasedHandler onPerkPurchasedE;
		
		public bool singleLevelOnly=false;
		public bool saveProgress=false;
		
		public List<int> unavailableIDList=new List<int>(); 	//ID list of perk available for this level, modified in editor
		public List<int> purchasedIDList=new List<int>(); 	//ID list of perk pre-purcahsed for this level, modified in editor
		private List<Perk> perkList=new List<Perk>();
		public static List<Perk> GetPerkList(){ return instance.perkList; }
		public static int GetPerkListCount(){ return instance.perkList.Count; }
		
		public int perkCurrency=0;
		public int currencyGainOnWin=3;
		public static int GetPerkCurrency(){ return instance.perkCurrency; }
		public static void SpendCurrency(int val){ 
			instance.perkCurrency-=val; 
			if(onPerkCurrencyE!=null) onPerkCurrencyE(instance.perkCurrency);
		}
		public static void GainPerkCurrencyOnVictory(){ SpendCurrency(instance!=null ? -instance.currencyGainOnWin : 0); }
		
		
		
		public int perkPoint;
		public static int GetPerkPoint(){ return instance.perkPoint; }
		
		
		
		private List<int> factionAbilityIDList=new List<int>();	//all IDs of FactionAbilities unlocked via perk
		public static List<int> GetFactionAbilityIDList(){ return instance!=null ? instance.factionAbilityIDList : new List<int>(); }
		
		
		
		private static PerkManager instance;
		public static bool IsOn(){ return instance==null ? false : true; }
		
		void Awake(){
			if(instance==null) instance=this;
			else Destroy(gameObject);
			
			if(!singleLevelOnly) DontDestroyOnLoad(gameObject);
			
			Init();
		}
		
		private bool init=false;
		public void Init(){
			if(init) return;
			
			init=true;
			
			if(instance==null) instance=this;
			
			//loading the perks from TB
			if(singleLevelOnly){
				List<Perk> dbList=PerkDB.Load();
				for(int i=0; i<dbList.Count; i++){
					if(!unavailableIDList.Contains(dbList[i].prefabID)){
						Perk perk=dbList[i].Clone();
						perkList.Add(perk);
						if(purchasedIDList.Contains(perk.prefabID)) perk.purchased=true;
					}
				}
			}
			else{
				perkList=PerkDB.LoadClone();
				if(saveProgress) LoadPerkProgress();
			}
			
			for(int i=0; i<perkList.Count; i++){
				if(perkList[i].purchased) _PurchasePerk(perkList[i], false);	//dont use currency since these are pre-purchased perk
			}
		}
		

		
		
		void OnLevelWasLoaded(){
			//if(saveProgress) SavePerkProgress();
		}
		
		
		public static Perk GetPerk(int perkID){ return instance._GetPerk(perkID); }
		public Perk _GetPerk(int perkID){
			for(int i=0; i<perkList.Count; i++){ if(perkList[i].prefabID==perkID) return perkList[i]; }
			return null;
		}
		public static string IsPerkAvailable(int perkID){ return instance._IsPerkAvailable(perkID); }
		public string _IsPerkAvailable(int perkID){
			for(int i=0; i<perkList.Count; i++){ if(perkList[i].prefabID==perkID) return perkList[i].IsAvailable(); }
			return "PerkID doesnt correspond to any perk in the list   "+perkID;
		}
		public static bool IsPerkPurchased(int perkID){ return instance._IsPerkPurchased(perkID); }
		public bool _IsPerkPurchased(int perkID){
			for(int i=0; i<perkList.Count; i++){ if(perkList[i].prefabID==perkID) return perkList[i].purchased; }
			return false;
		}
		
		
		
		
		public static string PurchasePerk(int perkID, bool useCurrency=true){ return instance._PurchasePerk(perkID, useCurrency); }
		public string _PurchasePerk(int perkID, bool useCurrency=true){ 
			for(int i=0; i<perkList.Count; i++){ if(perkList[i].prefabID==perkID) return instance._PurchasePerk(perkList[i], useCurrency); }
			return "PerkID doesnt correspond to any perk in the list";
		}
		
		public static string PurchasePerk(Perk perk, bool useCurrency=true){ return instance._PurchasePerk(perk, useCurrency); }
		
		public string _PurchasePerk(Perk perk, bool useCurrency=true){
			string text=perk.Purchase(useCurrency); 
			if(text!="") return text;
			
			if(saveProgress) SavePerkProgress();
			
			if(onPerkPurchasedE!=null) onPerkPurchasedE(perk);
			
			//process the prereq for other perk
			for(int i=0; i<perkList.Count; i++){
				Perk perkTemp=perkList[i];
				if(perkTemp.purchased || perkTemp.prereq.Count==0) continue;
				perkTemp.prereq.Remove(perk.prefabID);
			}
			
			
			perkPoint+=1;
			if(onPerkPointE!=null) onPerkPointE(perkPoint);
			
			
			if(perk.type==_PerkType.NewUnitAbility){
				AbilityManagerUnit.PerkUnlockNewAbility(perk.itemIDList[0], perk.abilityID);
				if(perk.addAbilityToAllUnit){
					globalUnitModifier.abilityIDList.Add(perk.abilityID);
				}
				else{
					int index=UnitModifierExist(perk.itemIDList[0]);
					if(index==-1){
						PerkUnitModifier unitModifier=new PerkUnitModifier();
						unitModifier.prefabID=perk.itemIDList[0];
						unitModifierList.Add(unitModifier);
						index=unitModifierList.Count-1;
					}
					unitModifierList[index].abilityIDList.Add(perk.abilityID);
				}
			}
			else if(perk.type==_PerkType.NewFactionAbility){ 
				AbilityManagerFaction.PerkUnlockNewAbility(perk.abilityID);
				factionAbilityIDList.Add(perk.abilityID);
			}
			else if(perk.type==_PerkType.Unit){ 
				for(int i=0; i<perk.itemIDList.Count; i++){
					int index=UnitModifierExist(perk.itemIDList[i]);
					if(index==-1){
						PerkUnitModifier unitModifier=new PerkUnitModifier();
						unitModifier.prefabID=perk.itemIDList[i];
						unitModifierList.Add(unitModifier);
						index=unitModifierList.Count-1;
					}
					ModifyUnitModifierInList(index, perk);
				}
			}
			else if(perk.type==_PerkType.AllUnit){ ModifyUnitModifier(globalUnitModifier, perk); }
			else if(perk.type==_PerkType.UnitAbility){ 
				for(int i=0; i<perk.itemIDList.Count; i++){
					int index=UnitAbilityModifierExist(perk.itemIDList[i]);
					if(index==-1){
						PerkAbilityModifier abilityModifier=new PerkAbilityModifier();
						abilityModifier.prefabID=perk.itemIDList[i];
						unitAbilityModifierList.Add(abilityModifier);
						index=unitAbilityModifierList.Count-1;
					}
					ModifyUnitAbilityModifierInList(index, perk);
				}
			}
			else if(perk.type==_PerkType.AllUnitAbility){ ModifyUnitAbilityModifier(globalUnitAbilityModifier, perk); }
			else if(perk.type==_PerkType.FactionAbility){ 
				for(int i=0; i<perk.itemIDList.Count; i++){
					int index=FactionAbilityModifierExist(perk.itemIDList[i]);
					if(index==-1){
						PerkAbilityModifier abilityModifier=new PerkAbilityModifier();
						abilityModifier.prefabID=perk.itemIDList[i];
						factionAbilityModifierList.Add(abilityModifier);
						index=factionAbilityModifierList.Count-1;
					}
					ModifyFactionAbilityModifierInList(index, perk);
				}
			}
			else if(perk.type==_PerkType.AllFactionAbility){ ModifyFactionAbilityModifier(globalFactionAbilityModifier, perk); }
			
			
			return "";
		}
		
		
		
		
		
		
		
		
		public static PerkUnitModifier emptyUnitModifier=new PerkUnitModifier();	//always zero, for unit without any perk bonus
		public static PerkUnitModifier globalUnitModifier=new PerkUnitModifier();	//for all unit
		public static List<PerkUnitModifier> unitModifierList=new List<PerkUnitModifier>();		//for a particular unit
		
		private int UnitModifierExist(int prefabID){
			for(int i=0; i<unitModifierList.Count; i++){ if(unitModifierList[i].prefabID==prefabID) return i; }
			return -1;
		}
		private void ModifyUnitModifierInList(int index, Perk perk){ ModifyUnitModifier(unitModifierList[index], perk); }
		private void ModifyUnitModifier(PerkUnitModifier unitModifier, Perk perk){
			ModifyUnitStats(unitModifier.stats, perk.stats);
		}
		
		public static PerkUnitModifier GetUnitModifier(int prefabID){
			for(int i=0; i<unitModifierList.Count; i++){
				if(unitModifierList[i].prefabID==prefabID) return unitModifierList[i];
			}
			return emptyUnitModifier;
		}
		
		public static float GetUnitHPBuff(int prefabID){ return globalUnitModifier.stats.APBuff+GetUnitModifier(prefabID).stats.HPBuff; }
		public static float GetUnitAPBuff(int prefabID){ return globalUnitModifier.stats.APBuff+GetUnitModifier(prefabID).stats.APBuff; }
		
		public static float GetUnitMoveAPCost(int prefabID){ return globalUnitModifier.stats.moveAPCost+GetUnitModifier(prefabID).stats.moveAPCost; }
		public static float GetUnitAttackAPCost(int prefabID){ return globalUnitModifier.stats.attackAPCost+GetUnitModifier(prefabID).stats.attackAPCost; }
		
		public static float GetUnitTurnPriority(int prefabID){ return globalUnitModifier.stats.turnPriority+GetUnitModifier(prefabID).stats.turnPriority; }
		
		public static int GetUnitMoveRange(int prefabID){ return globalUnitModifier.stats.moveRange+GetUnitModifier(prefabID).stats.moveRange; }
		public static int GetUnitAttackRange(int prefabID){ return globalUnitModifier.stats.attackRange+GetUnitModifier(prefabID).stats.attackRange; }
		public static int GetUnitSight(int prefabID){ return globalUnitModifier.stats.sight+GetUnitModifier(prefabID).stats.sight; }
		
		public static int GetUnitMovePerTurn(int prefabID){ return globalUnitModifier.stats.movePerTurn+GetUnitModifier(prefabID).stats.movePerTurn; }
		public static int GetUnitAttackPerTurn(int prefabID){ return globalUnitModifier.stats.attackPerTurn+GetUnitModifier(prefabID).stats.attackPerTurn; }
		public static int GetUnitCounterPerTurn(int prefabID){ return globalUnitModifier.stats.counterPerTurn+GetUnitModifier(prefabID).stats.counterPerTurn; }
		
		public static float GetUnitDamage(int prefabID){ return globalUnitModifier.stats.damage+GetUnitModifier(prefabID).stats.damage; }
		//public static float GetUnitDamageMax(int prefabID){ return globalUnitModifier.stats.damageMax+GetUnitModifier(prefabID).stats.damageMax; }
		
		public static float GetUnitHitChance(int prefabID){ return globalUnitModifier.stats.hitChance+GetUnitModifier(prefabID).stats.hitChance; }
		public static float GetUnitDodgeChance(int prefabID){ return globalUnitModifier.stats.dodgeChance+GetUnitModifier(prefabID).stats.dodgeChance; }
		
		public static float GetUnitCritChance(int prefabID){ return globalUnitModifier.stats.critChance+GetUnitModifier(prefabID).stats.critChance; }
		public static float GetUnitCritAvoidance(int prefabID){ return globalUnitModifier.stats.critAvoidance+GetUnitModifier(prefabID).stats.critAvoidance; }
		public static float GetUnitCritMultiplier(int prefabID){ return globalUnitModifier.stats.critMultiplier+GetUnitModifier(prefabID).stats.critMultiplier; }
		
		public static float GetUnitStunChance(int prefabID){ return globalUnitModifier.stats.stunChance+GetUnitModifier(prefabID).stats.stunChance; }
		public static float GetUnitStunAvoidance(int prefabID){ return globalUnitModifier.stats.stunAvoidance+GetUnitModifier(prefabID).stats.stunAvoidance; }
		public static int GetUnitStunDuration(int prefabID){ return globalUnitModifier.stats.stunDuration+GetUnitModifier(prefabID).stats.stunDuration; }
		
		public static float GetUnitSilentChance(int prefabID){ return globalUnitModifier.stats.stunChance+GetUnitModifier(prefabID).stats.silentChance; }
		public static float GetUnitSilentAvoidance(int prefabID){ return globalUnitModifier.stats.silentAvoidance+GetUnitModifier(prefabID).stats.silentAvoidance; }
		public static int GetUnitSilentDuration(int prefabID){ return globalUnitModifier.stats.silentDuration+GetUnitModifier(prefabID).stats.silentDuration; }
		
		public static float GetUnitFlankingBonus(int prefabID){ return globalUnitModifier.stats.flankingBonus+GetUnitModifier(prefabID).stats.flankingBonus; }
		public static float GetUnitFlankedModifier(int prefabID){ return globalUnitModifier.stats.flankedModifier+GetUnitModifier(prefabID).stats.flankedModifier; }
		
		public static float GetUnitHPPerTurn(int prefabID){ return globalUnitModifier.stats.HPPerTurn+GetUnitModifier(prefabID).stats.HPPerTurn; }
		public static float GetUnitAPPerTurn(int prefabID){ return globalUnitModifier.stats.APPerTurn+GetUnitModifier(prefabID).stats.APPerTurn; }
		
		public static List<int> GetUnitAbilityIDList(int prefabID){
			List<int> abilityIDList=new List<int>();
			for(int i=0; i<globalUnitModifier.abilityIDList.Count; i++) abilityIDList.Add(globalUnitModifier.abilityIDList[i]);
			
			PerkUnitModifier unitModifier=GetUnitModifier(prefabID);
			for(int i=0; i<unitModifier.abilityIDList.Count; i++) abilityIDList.Add(unitModifier.abilityIDList[i]);
			
			return abilityIDList;
		}
		
		
		
		
		
		
		public static PerkAbilityModifier emptyAbilityModifier=new PerkAbilityModifier();	//always zero, for both unit and faction ability without any perk bonus
		public static PerkAbilityModifier globalUnitAbilityModifier=new PerkAbilityModifier();	//for all unit
		public static List<PerkAbilityModifier> unitAbilityModifierList=new List<PerkAbilityModifier>();		//for a particular unit
		
		private int UnitAbilityModifierExist(int prefabID){
			for(int i=0; i<unitAbilityModifierList.Count; i++){ if(unitAbilityModifierList[i].prefabID==prefabID) return i; }
			return -1;
		}
		private void ModifyUnitAbilityModifierInList(int index, Perk perk){ ModifyUnitAbilityModifier(unitAbilityModifierList[index], perk); }
		private void ModifyUnitAbilityModifier(PerkAbilityModifier abilityModifier, Perk perk){
			ModifyAbilityModifier(abilityModifier, perk);
			ModifyUnitStats(abilityModifier.stats, perk.stats);
		}
		
		public static PerkAbilityModifier GetUnitAbilityModifier(int prefabID){
			for(int i=0; i<unitAbilityModifierList.Count; i++){
				if(unitAbilityModifierList[i].prefabID==prefabID) return unitAbilityModifierList[i];
			}
			return emptyAbilityModifier;
		}
		
		
		
		
		
		
		
		
		
		public static PerkAbilityModifier globalFactionAbilityModifier=new PerkAbilityModifier();	//for all unit
		public static List<PerkAbilityModifier> factionAbilityModifierList=new List<PerkAbilityModifier>();		//for a particular unit
		
		private int FactionAbilityModifierExist(int prefabID){
			for(int i=0; i<factionAbilityModifierList.Count; i++){ if(unitAbilityModifierList[i].prefabID==prefabID) return i; }
			return -1;
		}
		private void ModifyFactionAbilityModifierInList(int index, Perk perk){ ModifyFactionAbilityModifier(factionAbilityModifierList[index], perk); }
		private void ModifyFactionAbilityModifier(PerkAbilityModifier abilityModifier, Perk perk){
			ModifyAbilityModifier(abilityModifier, perk);
			ModifyUnitStats(abilityModifier.stats, perk.stats);
		}
		
		public static PerkAbilityModifier GetFactionAbilityModifier(int prefabID){
			for(int i=0; i<factionAbilityModifierList.Count; i++){
				if(factionAbilityModifierList[i].prefabID==prefabID) return factionAbilityModifierList[i];
			}
			return emptyAbilityModifier;
		}
		
		
		
		
		public static int GetAbilityDuration(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.duration+GetUnitAbilityModifier(prefabID).duration; 
			else return globalFactionAbilityModifier.duration+GetFactionAbilityModifier(prefabID).duration;
		}
		public static float GetAbilityCost(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.cost+GetUnitAbilityModifier(prefabID).cost;
			else return globalFactionAbilityModifier.cost+GetFactionAbilityModifier(prefabID).cost;
		}
		public static int GetAbilityCD(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.cooldown+GetUnitAbilityModifier(prefabID).cooldown; 
			else return globalFactionAbilityModifier.cooldown+GetFactionAbilityModifier(prefabID).cooldown;
		}
		public static int GetAbilityUseLimit(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.useLimit+GetUnitAbilityModifier(prefabID).useLimit; 
			else return globalFactionAbilityModifier.useLimit+GetFactionAbilityModifier(prefabID).useLimit;
		}
		public static int GetAbilityRange(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.range+GetUnitAbilityModifier(prefabID).range; 
			else return globalFactionAbilityModifier.range+GetFactionAbilityModifier(prefabID).range;
		}
		public static int GetAbilityAOERange(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.aoeRange+GetUnitAbilityModifier(prefabID).aoeRange; 
			else return globalFactionAbilityModifier.aoeRange+GetFactionAbilityModifier(prefabID).aoeRange;
		}
		
		public static float GetAbilityHP(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.HP+GetUnitAbilityModifier(prefabID).HP; 
			else return globalFactionAbilityModifier.HP+GetFactionAbilityModifier(prefabID).HP;
		}
		//public static float GetAbilityHPMax(int prefabID, bool faction=false){ 
		//	if(!faction) return globalUnitAbilityModifier.HPMax+GetUnitAbilityModifier(prefabID).HPMax; 
		//	else return globalFactionAbilityModifier.HPMax+GetFactionAbilityModifier(prefabID).HPMax;
		//}
		public static float GetAbilityAP(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.AP+GetUnitAbilityModifier(prefabID).AP; 
			else return globalFactionAbilityModifier.AP+GetFactionAbilityModifier(prefabID).AP;
		}
		//public static float GetAbilityAPMax(int prefabID, bool faction=false){ 
		//	if(!faction) return globalUnitAbilityModifier.APMax+GetUnitAbilityModifier(prefabID).APMax; 
		//	else return globalFactionAbilityModifier.APMax+GetFactionAbilityModifier(prefabID).APMax;
		//}
		public static int GetAbilityStun(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stun+GetUnitAbilityModifier(prefabID).stun; 
			else return globalFactionAbilityModifier.stun+GetFactionAbilityModifier(prefabID).stun;
		}
		
		public static float GetAbilityHPBuff(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.APBuff+GetUnitAbilityModifier(prefabID).stats.HPBuff;
			else return globalFactionAbilityModifier.stats.HPBuff+GetFactionAbilityModifier(prefabID).stats.HPBuff;
		}
		public static float GetAbilityAPBuff(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.APBuff+GetUnitAbilityModifier(prefabID).stats.APBuff;
			else return globalFactionAbilityModifier.stats.APBuff+GetFactionAbilityModifier(prefabID).stats.APBuff;
		}
		
		public static float GetAbilityMoveAPCost(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.moveAPCost+GetUnitAbilityModifier(prefabID).stats.moveAPCost;
			else return globalFactionAbilityModifier.stats.moveAPCost+GetFactionAbilityModifier(prefabID).stats.moveAPCost;
		}
		public static float GetAbilityAttackAPCost(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.attackAPCost+GetUnitAbilityModifier(prefabID).stats.attackAPCost;
			else return globalFactionAbilityModifier.stats.attackAPCost+GetFactionAbilityModifier(prefabID).stats.attackAPCost;
		}
		
		public static float GetAbilityTurnPriority(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.turnPriority+GetUnitAbilityModifier(prefabID).stats.turnPriority;
			else return globalFactionAbilityModifier.stats.turnPriority+GetFactionAbilityModifier(prefabID).stats.turnPriority;
		}
		
		public static int GetAbilityMoveRange(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.moveRange+GetUnitAbilityModifier(prefabID).stats.moveRange;
			else return globalFactionAbilityModifier.stats.moveRange+GetFactionAbilityModifier(prefabID).stats.moveRange;
		}
		public static int GetAbilityAttackRange(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.attackRange+GetUnitAbilityModifier(prefabID).stats.attackRange;
			else return globalFactionAbilityModifier.stats.attackRange+GetFactionAbilityModifier(prefabID).stats.attackRange;
		}
		public static int GetAbilitySight(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.sight+GetUnitAbilityModifier(prefabID).stats.sight;
			else return globalFactionAbilityModifier.stats.sight+GetFactionAbilityModifier(prefabID).stats.sight;
		}
		
		public static int GetAbilityMovePerTurn(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.movePerTurn+GetUnitAbilityModifier(prefabID).stats.movePerTurn;
			else return globalFactionAbilityModifier.stats.movePerTurn+GetFactionAbilityModifier(prefabID).stats.movePerTurn;
		}
		public static int GetAbilityAttackPerTurn(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.attackPerTurn+GetUnitAbilityModifier(prefabID).stats.attackPerTurn;
			else return globalFactionAbilityModifier.stats.attackPerTurn+GetFactionAbilityModifier(prefabID).stats.attackPerTurn;
		}
		public static int GetAbilityCounterPerTurn(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.counterPerTurn+GetUnitAbilityModifier(prefabID).stats.counterPerTurn;
			else return globalFactionAbilityModifier.stats.counterPerTurn+GetFactionAbilityModifier(prefabID).stats.counterPerTurn;
		}
		
		public static float GetAbilityDamage(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.damage+GetUnitAbilityModifier(prefabID).stats.damage;
			else return globalFactionAbilityModifier.stats.damage+GetFactionAbilityModifier(prefabID).stats.damage;
		}
		//public static float GetAbilityDamageMax(int prefabID, bool faction=false){ 
		//	if(!faction) return globalUnitAbilityModifier.stats.damageMax+GetUnitAbilityModifier(prefabID).stats.damageMax;
		//	else return globalFactionAbilityModifier.stats.damageMax+GetFactionAbilityModifier(prefabID).stats.damageMax;
		//}
		
		public static float GetAbilityHitChance(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.hitChance+GetUnitAbilityModifier(prefabID).stats.hitChance;
			else return globalFactionAbilityModifier.stats.hitChance+GetFactionAbilityModifier(prefabID).stats.hitChance;
		}
		public static float GetAbilityDodgeChance(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.dodgeChance+GetUnitAbilityModifier(prefabID).stats.dodgeChance;
			else return globalFactionAbilityModifier.stats.dodgeChance+GetFactionAbilityModifier(prefabID).stats.dodgeChance;
		}
		
		public static float GetAbilityCritChance(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.critChance+GetUnitAbilityModifier(prefabID).stats.critChance;
			else return globalFactionAbilityModifier.stats.critChance+GetFactionAbilityModifier(prefabID).stats.critChance;
		}
		public static float GetAbilityCritAvoidance(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.critAvoidance+GetUnitAbilityModifier(prefabID).stats.critAvoidance;
			else return globalFactionAbilityModifier.stats.critAvoidance+GetFactionAbilityModifier(prefabID).stats.critAvoidance;
		}
		public static float GetAbilityCritMultiplier(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.critMultiplier+GetUnitAbilityModifier(prefabID).stats.critMultiplier;
			else return globalFactionAbilityModifier.stats.critMultiplier+GetFactionAbilityModifier(prefabID).stats.critMultiplier;
		}
		
		public static float GetAbilityStunChance(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.stunChance+GetUnitAbilityModifier(prefabID).stats.stunChance;
			else return globalFactionAbilityModifier.stats.stunChance+GetFactionAbilityModifier(prefabID).stats.stunChance;
		}
		public static float GetAbilityStunAvoidance(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.stunAvoidance+GetUnitAbilityModifier(prefabID).stats.stunAvoidance;
			else return globalFactionAbilityModifier.stats.stunAvoidance+GetFactionAbilityModifier(prefabID).stats.stunAvoidance;
		}
		public static int GetAbilityStunDuration(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.stunDuration+GetUnitAbilityModifier(prefabID).stats.stunDuration;
			else return globalFactionAbilityModifier.stats.stunDuration+GetFactionAbilityModifier(prefabID).stats.stunDuration;
		}
		
		public static float GetAbilitySilentChance(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.silentChance+GetUnitAbilityModifier(prefabID).stats.silentChance;
			else return globalFactionAbilityModifier.stats.silentChance+GetFactionAbilityModifier(prefabID).stats.silentChance;
		}
		public static float GetAbilitySilentAvoidance(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.silentAvoidance+GetUnitAbilityModifier(prefabID).stats.silentAvoidance;
			else return globalFactionAbilityModifier.stats.silentAvoidance+GetFactionAbilityModifier(prefabID).stats.silentAvoidance;
		}
		public static int GetAbilitySilentDuration(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.silentDuration+GetUnitAbilityModifier(prefabID).stats.silentDuration;
			else return globalFactionAbilityModifier.stats.silentDuration+GetFactionAbilityModifier(prefabID).stats.silentDuration;
		}
		
		public static float GetAbilityFlankingBonus(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.flankingBonus+GetUnitAbilityModifier(prefabID).stats.flankingBonus;
			else return globalFactionAbilityModifier.stats.flankingBonus+GetFactionAbilityModifier(prefabID).stats.flankingBonus;
		}
		public static float GetAbilityFlankedModifier(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.flankedModifier+GetUnitAbilityModifier(prefabID).stats.flankedModifier;
			else return globalFactionAbilityModifier.stats.flankedModifier+GetFactionAbilityModifier(prefabID).stats.flankedModifier;
		}
		
		public static float GetAbilityHpPerTurn(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.HPPerTurn+GetUnitAbilityModifier(prefabID).stats.HPPerTurn;
			else return globalFactionAbilityModifier.stats.HPPerTurn+GetFactionAbilityModifier(prefabID).stats.HPPerTurn;
		}
		public static float GetAbilityAPPerTurn(int prefabID, bool faction=false){ 
			if(!faction) return globalUnitAbilityModifier.stats.APPerTurn+GetUnitAbilityModifier(prefabID).stats.APPerTurn;
			else return globalFactionAbilityModifier.stats.APPerTurn+GetFactionAbilityModifier(prefabID).stats.APPerTurn;
		}
		
		
		
		private void ModifyAbilityModifier(PerkAbilityModifier modifier, Perk perk){
			modifier.duration+=perk.abDurationMod;
			modifier.cost+=perk.abCostMod;
			modifier.cooldown+=perk.abCooldownMod;
			modifier.useLimit+=perk.abUseLimitMod;
			modifier.range+=perk.abRangeMod;
			modifier.aoeRange+=perk.abAOERangeMod;
			modifier.HP+=perk.abHPMod;
			//modifier.HPMax+=perk.abHPMaxMod;
			modifier.AP+=perk.abAPMod;
			//modifier.APMax+=perk.abAPMaxMod;
			modifier.stun+=perk.abStunMod;
		}
		private void ModifyUnitStats(UnitStat tgtStats, UnitStat srcStats){
			tgtStats.HPBuff+=srcStats.HPBuff;
			tgtStats.APBuff+=srcStats.APBuff;
			tgtStats.moveAPCost+=srcStats.moveAPCost;
			tgtStats.attackAPCost+=srcStats.attackAPCost;
			tgtStats.turnPriority+=srcStats.turnPriority;
			tgtStats.moveRange+=srcStats.moveRange;
			tgtStats.attackRange+=srcStats.attackRange;
			tgtStats.hitChance+=srcStats.hitChance;
			tgtStats.dodgeChance+=srcStats.dodgeChance;
			tgtStats.damage+=srcStats.damage;
			//tgtStats.damageMin+=srcStats.damageMin;
			//tgtStats.damageMax+=srcStats.damageMax;
			tgtStats.movePerTurn+=srcStats.movePerTurn;
			tgtStats.attackPerTurn+=srcStats.attackPerTurn;
			tgtStats.counterPerTurn+=srcStats.counterPerTurn;
			tgtStats.critChance+=srcStats.critChance;
			tgtStats.critAvoidance+=srcStats.critAvoidance;
			tgtStats.critMultiplier+=srcStats.critMultiplier;
			tgtStats.stunChance+=srcStats.stunChance;
			tgtStats.stunAvoidance+=srcStats.stunAvoidance;
			tgtStats.stunDuration+=srcStats.stunDuration;
			tgtStats.HPPerTurn+=srcStats.HPPerTurn;
			tgtStats.APPerTurn+=srcStats.APPerTurn;
			tgtStats.sight+=srcStats.sight;
		}
		
		
		
		
		public static void ResetPerkPoint(){ instance._ResetPerkPoint(); }
		public void _ResetPerkPoint(){
			perkPoint=0;
			for(int i=0; i<perkList.Count; i++){
				if(perkList[i].purchased) perkPoint+=1;
			}
		}
		
		
		public static void LoadPerkProgress(){ instance._LoadPerkProgress(); }
		public void _LoadPerkProgress(){
			perkCurrency=PlayerPrefs.GetInt("TBTK_PerkCurrency", perkCurrency);
			for(int i=0; i<perkList.Count; i++){
				if(PlayerPrefs.GetInt("TBTK_Perk_"+perkList[i].name, 0)==1){
					perkList[i].purchased=true;
				}
			}
		}
		
		public static void SavePerkProgress(){ instance._SavePerkProgress(); }
		public void _SavePerkProgress(){
			PlayerPrefs.SetInt("TBTK_PerkCurrency", perkCurrency);
			for(int i=0; i<perkList.Count; i++){
				PlayerPrefs.SetInt("TBTK_Perk_"+perkList[i].name, perkList[i].purchased ? 1 : 0);
			}
		}
		
		public static void ClearPerkProgress(){ instance._ClearPerkProgress(); }
		public void _ClearPerkProgress(){
			PlayerPrefs.DeleteKey("TBTK_PerkCurrency");
			for(int i=0; i<perkList.Count; i++){
				PlayerPrefs.DeleteKey("TBTK_Perk_"+perkList[i].name);
			}
		}
		
	}
	
	
}
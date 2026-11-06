using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private enum GarageOverlay { None, Setups, Compatibility, Sell, Overwrite }
        private GarageOverlay garageOverlay;
        private bool garageOwnedOnly,garageCategoryFocus;
        private int garageTireFilter,garageSetupIndex;
        private List<GaragePart> garageCandidates=new List<GaragePart>();
        private GaragePart garageRecommendation;
        private int garagePlannedContractIndex=-1;
        private string garageActiveSetupName;
        private RecoveryContract GarageAdviceContract()
        {
            if(session.World.HasContract)return session.World.Contract;
            var contracts=session.World.AvailableContracts;
            int index=garagePlannedContractIndex;
            if(index<0||index>=contracts.Length)
            {var available=RecoveryBoardEntries.Select(contracts,P.completedMask,false);index=available.Count>0?available[0]:-1;}
            return index>=0?contracts[index]:null;
        }
        private readonly GarageSlot[] garageRememberedSlots={GarageSlot.Bonnet,GarageSlot.Wheels,GarageSlot.Cable};
        private bool CanTransactGarage=>session.GarageOpen&&session.Connected&&session.LocalSlot>=0&&session.World.Phase==RecoveryPhase.Explore;
        private bool CanFitGaragePart(GaragePart part)=>part!=null&&CanTransactGarage&&RecoveryGarageWorkflow.State(P,part,session.GarageCatalog)!=GarageItemState.Equipped&&
            (part.Price==0||P.Owns(part.Id)||P.credits>=RecoveryEconomy.Price(P,part));
        private string GaragePrimaryLabel(GaragePart part)
        {
            if(part==null)return "NO MATCHING PARTS";
            var state=RecoveryGarageWorkflow.State(P,part,session.GarageCatalog);
            if(state==GarageItemState.Equipped)return "EQUIPPED";
            if(state==GarageItemState.Owned)return "FIT PART · FREE";
            int cost=RecoveryEconomy.Price(P,part);
            return cost>P.credits?"NEED "+(cost-P.credits).ToString("N0")+" CR":"BUY & FIT · "+cost.ToString("N0")+" CR";
        }
        private void RememberGarageScroll()
        {if(garageListScroll!=null)garageScrollY=garageListScroll.content.anchoredPosition.y;}
        private void SelectGaragePart(string id)
        {
            bool changed=garageSelected!=id||garageCategoryFocus;garageCategoryFocus=false;
            if(!changed)return;
            float speed=garageListScroll!=null?garageListScroll.ScrollSpeed:0;
            RememberGarageScroll();garageSelected=id;garageMessage="";
            garageDraft=RecoveryGarageWorkflow.Preview(P.Build(P.vehicle),session.GarageCatalog.Find(id),session.GarageCatalog);
            session.PreviewGarage(garageDraft);Show("Garage");
            if(garageListScroll!=null)garageListScroll.ScrollSpeed=speed;
        }
        private void SelectGarageSlot(GarageSlot slot,bool categories=false)
        {
            garageSlot=slot;garageSection=RecoveryGarageWorkflow.Section(slot);garageRememberedSlots[garageSection]=slot;
            garageCategoryFocus=categories;garageScrollY=0;garageMessage="";
            garageSelected=P.Build(P.vehicle).Item(slot,session.GarageCatalog);garageDraft=P.Build(P.vehicle).Copy();
            session.PreviewGarage(garageDraft);Show("Garage");
        }
        private void SelectGarageSection(int section)
        {SelectGarageSlot(garageRememberedSlots[Mathf.Clamp(section,0,2)]);}
        private void ToggleGarageOwned()
        {RememberGarageScroll();garageOwnedOnly=!garageOwnedOnly;garageScrollY=0;Show("Garage");}
        private void CycleGarageTires(int step)
        {if(garageSlot!=GarageSlot.Wheels)return;garageTireFilter=(garageTireFilter+step+6)%6;garageScrollY=0;Show("Garage");}
        private void JumpGarageRecommendation()
        {
            if(garageRecommendation==null)return;var id=garageRecommendation.Id;var slot=garageRecommendation.Slot;
            garageOwnedOnly=false;garageTireFilter=0;SelectGarageSlot(slot);SelectGaragePart(id);
        }
        private void GaragePrimaryAction()
        {
            var part=session.GarageCatalog.Find(garageSelected);if(!CanFitGaragePart(part))return;
            session.ConsumeMenuInput();
            if(RecoveryGarageWorkflow.ReplacedParts(P.Build(P.vehicle),garageDraft,part,session.GarageCatalog).Count>0)
            {OpenGarageOverlay(GarageOverlay.Compatibility);return;}
            FitGaragePart();
        }
        private void FitGaragePart()
        {
            if(!CanTransactGarage)return;
            var catalog=session.GarageCatalog;var part=catalog.Find(garageSelected);
            if(!RecoveryGarageWorkflow.TryPrepareFit(P,part,catalog,out var build))return;
            session.ConsumeMenuInput();session.CommitGarage(build);
            garageDraft=P.Build(P.vehicle).Copy();garageOverlay=GarageOverlay.None;
            garageMessage=GarageName(part)+" fitted and saved";garageSubmitRelease=true;Show("Garage");
        }
        private void CloseGarage()
        {
            if(session.Training&&session.TrainingStep>=6){session.ConsumeMenuInput();session.EndTraining(true);Show("TrainingComplete");return;}
            session.ConsumeMenuInput();garageDraft=null;garageOverlay=GarageOverlay.None;session.GarageCameraInputBlocked=false;
            session.CloseOverlay();Show("Gameplay");
        }
        private void ResetGarageView(){session.ResetGarageView();garageMessage="Camera view reset";}
        private void OpenGarageOverlay(GarageOverlay target)
        {RememberGarageScroll();garageOverlay=target;garageDialogIndex=0;garageSubmitRelease=true;Show("Garage");}
        private void CloseGarageOverlay()
        {garageOverlay=GarageOverlay.None;garageSubmitRelease=true;Show("Garage");}
        private void RequestGarageSale()
        {
            var part=session.GarageCatalog.Find(garageSelected);
            if(!CanTransactGarage||part==null||part.Price==0||!P.Owns(part.Id)||RecoveryGarageWorkflow.InUseReason(P,part.Id)!=null)return;
            OpenGarageOverlay(GarageOverlay.Sell);
        }
        private void SellGaragePart()
        {
            if(!CanTransactGarage)return;var part=session.GarageCatalog.Find(garageSelected);
            if(!RecoveryEconomy.Sell(P,part,session.GarageCatalog))return;
            P.Save();session.ConsumeMenuInput();garageOverlay=GarageOverlay.None;
            garageSelected=P.Build(P.vehicle).Item(garageSlot,session.GarageCatalog);garageDraft=P.Build(P.vehicle).Copy();session.PreviewGarage(garageDraft);
            garageMessage="Part sold · "+(part.Price*70/100).ToString("N0")+" CR returned";garageSubmitRelease=true;Show("Garage");
        }
        private GaragePreset GarageSaved(int index)=>P.presets.Find(p=>p.Name==RecoveryGarageWorkflow.SetupKey(P.vehicle,index));
        private void RenameGarageSetup(int index)
        {
            if(!CanTransactGarage)return;
            var saved=GarageSaved(index);if(saved==null){saved=new GaragePreset{Name=RecoveryGarageWorkflow.SetupKey(P.vehicle,index)};P.presets.Add(saved);}
            string current=RecoveryGarageWorkflow.SetupLabel(saved,index);int next=Array.IndexOf(RecoveryGarageWorkflow.SetupNames,current)+1;
            saved.Label=RecoveryGarageWorkflow.SetupNames[next%RecoveryGarageWorkflow.SetupNames.Length];P.Save();Show("Garage");
        }
        private void RequestSaveGarageSetup(int index)
        {
            if(!CanTransactGarage)return;garageSetupIndex=index;
            if(GarageSaved(index)?.Build!=null)OpenGarageOverlay(GarageOverlay.Overwrite);else SaveGarageSetup();
        }
        private void SaveGarageSetup()
        {
            if(!CanTransactGarage)return;
            var saved=GarageSaved(garageSetupIndex);if(saved==null){saved=new GaragePreset{Name=RecoveryGarageWorkflow.SetupKey(P.vehicle,garageSetupIndex)};P.presets.Add(saved);}
            saved.Build=P.Build(P.vehicle).Copy();garageActiveSetupName=saved.Name;P.Save();garageOverlay=GarageOverlay.Setups;garageSubmitRelease=true;
            garageMessage=RecoveryGarageWorkflow.SetupLabel(saved,garageSetupIndex)+" fitted setup saved";Show("Garage");
        }
        private void ApplyGarageSetup(int index)
        {
            if(!CanTransactGarage)return;var saved=GarageSaved(index);if(saved?.Build==null)return;
            var build=saved.Build.Copy();build.Model=P.vehicle;build.Normalize(session.GarageCatalog,P.Owns);
            session.ConsumeMenuInput();session.CommitGarage(build);garageActiveSetupName=saved.Name;garageDraft=P.Build(P.vehicle).Copy();garageSelected=garageDraft.Item(garageSlot,session.GarageCatalog);
            garageMessage=RecoveryGarageWorkflow.SetupLabel(saved,index)+" setup fitted and saved";garageSubmitRelease=true;Show("Garage");
        }
        private void BuildGarageOverlay()
        {
            var shade=Panel(pageRoot,0,0,1600,900,new Color(0,0,0,.72f));shade.name="Garage modal blocker";shade.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            if(garageOverlay==GarageOverlay.Setups){BuildGarageSetups();return;}
            var panel=Panel(pageRoot,375,210,850,500);string title,body;
            if(garageOverlay==GarageOverlay.Compatibility)
            {
                title="REVIEW BODY CHANGE";
                var replaced=RecoveryGarageWorkflow.ReplacedParts(P.Build(P.vehicle),garageDraft,session.GarageCatalog.Find(garageSelected),session.GarageCatalog);
                body="These fitted parts do not fit this body and will return to Stock:\n\n"+string.Join("\n",replaced.Select(p=>"• "+SlotName(p.Slot)+": "+GarageName(p)))+"\n\nYou keep every replaced part in your inventory.";
            }
            else if(garageOverlay==GarageOverlay.Sell)
            {var p=session.GarageCatalog.Find(garageSelected);title="SELL PART";body="Sell "+GarageName(p)+" for "+(p.Price*70/100).ToString("N0")+" CR?\n\nYou'll need to buy it again to use it later.";}
            else{title="REPLACE SAVED SETUP";body="Replace setup "+(garageSetupIndex+1)+" ("+RecoveryGarageWorkflow.SetupLabel(GarageSaved(garageSetupIndex),garageSetupIndex)+") with your currently fitted setup?\n\nThe part you're only previewing will not be saved.";}
            Text(panel,title,28,24,794,45,29,Paper,true);
            var scrollRoot=Node("Review details",panel,28,88,794,286);var scroll=scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
            var viewport=Node("Review viewport",scrollRoot,0,0,794,286);viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color=Color.white;viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;
            var content=Node("Review text",viewport,0,0,774,286);var text=Text(content,body,0,0,774,286,18,Muted);text.enableAutoSizing=false;float height=Mathf.Max(286,text.GetPreferredValues(body,774,0).y);content.sizeDelta=new Vector2(774,height);text.rectTransform.sizeDelta=content.sizeDelta;scroll.viewport=viewport;scroll.content=content;
            garageReviewScroll=scroll;
            garageDialogHint=Text(panel,"",28,386,794,24,12,Muted);
            Action yes=garageOverlay==GarageOverlay.Compatibility?(Action)FitGaragePart:garageOverlay==GarageOverlay.Sell?SellGaragePart:SaveGarageSetup;
            var confirm=GarageOverlayButton(panel,garageOverlay==GarageOverlay.Compatibility?GaragePrimaryLabel(session.GarageCatalog.Find(garageSelected)):"CONFIRM",28,418,385,yes,true);
            AddGarageHint(confirm,"confirm");
            var cancel=GarageOverlayButton(panel,"CANCEL",437,418,385,()=>{if(garageOverlay==GarageOverlay.Overwrite){garageOverlay=GarageOverlay.Setups;garageSubmitRelease=true;Show("Garage");}else CloseGarageOverlay();});AddGarageHint(cancel,"back");
        }
        private void BuildGarageSetups()
        {
            var panel=Panel(pageRoot,265,137,1070,626);
            Text(panel,"SAVED SETUPS",26,20,1018,45,31,Paper,true);
            Text(panel,"Save your fitted equipment. Apply a saved setup for free. Select a name to cycle its label.",26,74,1018,44,16,Muted);
            var matches=Enumerable.Range(0,4).Select(GarageSaved).Where(p=>p?.Build!=null&&RecoveryGarageWorkflow.SameBuild(P.Build(P.vehicle),p.Build,session.GarageCatalog)).ToArray();
            var activePreset=matches.FirstOrDefault(p=>p.Name==garageActiveSetupName)??matches.FirstOrDefault();
            for(int i=0;i<4;i++)
            {
                int index=i;var saved=GarageSaved(i);string label=RecoveryGarageWorkflow.SetupLabel(saved,i);
                bool active=saved!=null&&saved==activePreset;
                var row=Panel(panel,24,126+i*100,1022,90);Text(row,(i+1)+" · "+label+" · "+(saved?.Build==null?"Empty":active?"Active":"Saved"),16,8,370,25,18,active?Amber:Paper,true);
                string summary="No saved equipment";
                if(saved?.Build!=null){var perf=GaragePerformance.Calculate(saved.Build,session.GarageCatalog);var tire=session.GarageCatalog.Find(saved.Build.Item(GarageSlot.Wheels,session.GarageCatalog));summary=(tire!=null?GarageName(tire):"Stock tires")+" · "+(perf.Cable/1000).ToString("0")+" kN cable";}
                Text(row,summary,16,41,370,37,13,Muted);
                var rename=GarageOverlayButton(row,"NAME",400,23,140,()=>RenameGarageSetup(index));rename.interactable=CanTransactGarage;
                var save=GarageOverlayButton(row,"SAVE FITTED",550,23,232,()=>RequestSaveGarageSetup(index));save.interactable=CanTransactGarage;
                var apply=GarageOverlayButton(row,active?"ACTIVE":"APPLY",792,23,214,()=>ApplyGarageSetup(index),active);apply.interactable=CanTransactGarage&&saved?.Build!=null&&!active;
                foreach(var button in new[]{rename,save,apply})GarageFont(button.GetComponentInChildren<TMP_Text>(),15);
            }
            var back=GarageOverlayButton(panel,"BACK TO GARAGE",24,552,400,CloseGarageOverlay);AddGarageHint(back,"back");
            garageDialogHint=Text(panel,"",446,548,598,54,13,Muted);
        }
    }
}

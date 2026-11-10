using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private GarageSlot garageSlot=GarageSlot.Wheels;
        private RecoveryBuild garageDraft;
        private string garageSelected,garageMessage="";
        private int boardIndex;
        private TMP_Text garagePrompt;
        private int garageSection=1;
        private string SlotName(GarageSlot slot)=>new[]{"BODY","WHEELS","BONNET","FRONT BUMPER","REAR BUMPER","FENDERS","ACCESSORIES","INTAKE","EXHAUST","CHASSIS","ENGINE","TRANSMISSION","SUSPENSION","CABLE","WINCH","PAINT"}[(int)slot];
        private string GarageName(GaragePart part)=>RecoveryGarageWorkflow.DisplayName(part,P,session.GarageCatalog);
        private void PrepareGarage()
        {
            garageDraft=P.Build(P.vehicle).Copy();garageDraft.Normalize(session.GarageCatalog,P.Owns);
            garageSelected=garageDraft.Item(garageSlot,session.GarageCatalog);garageMessage="";garageScrollY=0;
            garageOwnedOnly=false;garageTireFilter=0;garageOverlay=GarageOverlay.None;garageCategoryFocus=false;
        }
        private void BuildGarage()
        {
            var catalog=session.GarageCatalog;
            if(catalog==null){Header("GARAGE UNAVAILABLE","");return;}
            if(garageDraft==null)PrepareGarage();
            garageCards.Clear();garageCategoryButtons.Clear();garageHints.Clear();garageModalButtons.Clear();garageListScroll=null;
            Header("YOUR WORKSHOP",session.Training?"Practice only · preview equipment, then finish training. Your career money is safe.":"Preview a part. Fit it when you're ready.");Wallet(pageRoot,P.credits);
            var slots=RecoveryGarageWorkflow.Slots(garageSection);
            if(Array.IndexOf(slots,garageSlot)<0){garageSlot=slots[0];garageSelected=P.Build(P.vehicle).Item(garageSlot,catalog);garageScrollY=0;}
            var candidates=GarageCandidates();
            if(!candidates.Exists(p=>p.Id==garageSelected))
            {
                string fitted=P.Build(P.vehicle).Item(garageSlot,catalog);
                garageSelected=candidates.Exists(p=>p.Id==fitted)?fitted:candidates.Count>0?candidates[0].Id:null;
                garageDraft=RecoveryGarageWorkflow.Preview(P.Build(P.vehicle),catalog.Find(garageSelected),catalog);
                if(session.GarageOpen)session.PreviewGarage(garageDraft);
            }
            else garageDraft=RecoveryGarageWorkflow.Preview(P.Build(P.vehicle),catalog.Find(garageSelected),catalog);
            garageCandidates=candidates;
            if(candidates.Count==0)garageCategoryFocus=true;
            var selected=catalog.Find(garageSelected);
            session.FocusGarage(garageSlot,garageSelected);
            var fittedStats=GaragePerformance.Calculate(P.Build(P.vehicle),catalog);
            garageRecommendation=RecoveryGarageWorkflow.Recommend(GarageAdviceContract(),fittedStats,P.Build(P.vehicle),catalog,P);
            BuildGarageCategories(slots);
            BuildGarageSummary(selected);
            BuildGarageCatalog(candidates);
            BuildGarageComparisons(fittedStats,GaragePerformance.Calculate(garageDraft,catalog));
            BuildGarageActions(selected);
            BuildGarageFooter();
            if(garageOverlay!=GarageOverlay.None)BuildGarageOverlay();
            session.GarageCameraInputBlocked=garageOverlay!=GarageOverlay.None;
            FitNewPanelText();RefreshGarageHints();
        }
        private List<GaragePart> GarageCandidates()
        {
            var catalog=session.GarageCatalog;var fitted=P.Build(P.vehicle);var result=new List<GaragePart>();
            foreach(var part in catalog.Parts)
                if(part.Slot==garageSlot&&catalog.Compatible(part,fitted)&&(!garageOwnedOnly||part.Price==0||P.Owns(part.Id))&&
                    (garageSlot!=GarageSlot.Wheels||RecoveryGarageWorkflow.TireMatches(part,garageTireFilter)))result.Add(part);
            result.Sort((a,b)=>RecoveryGarageOrdering.Compare(a,b,P));return result;
        }
        private void BuildGarageCategories(GarageSlot[] slots)
        {
            var panel=Panel(pageRoot,36,212,GarageCategoryWidth,548);
            Text(panel,garageCategoryFocus?"CUSTOMIZE  >":"CUSTOMIZE",16,12,144,30,18,garageCategoryFocus?Amber:Paper,true);
            MenuGlyph(panel,"previous",168,11,60,32);MenuGlyph(panel,"next",236,11,60,32);
            string[] names={"STYLE","TECH","TOWING"};
            for(int i=0;i<3;i++)
            {
                int section=i;var tab=GarageButton(panel,names[i],12+i*94,61,88,()=>SelectGarageSection(section),garageSection==i,36);
                GarageFont(tab.GetComponentInChildren<TMP_Text>(),15);MarkOpenGarageTab(tab,garageSection==i);
            }
            for(int i=0;i<slots.Length;i++)
            {
                var choice=slots[i];var tab=GarageButton(panel,SlotName(choice),12,103+i*46,GarageCategoryWidth-24,()=>SelectGarageSlot(choice,true),garageSlot==choice,42);
                ButtonIcon(tab,SlotIcon(choice),garageSlot==choice);GarageFont(tab.GetComponentInChildren<TMP_Text>(),16);MarkOpenGarageTab(tab,garageSlot==choice,true);
                garageCategoryButtons[choice]=tab;
            }
            if(slots.Length<=6)
            {
                Panel(panel,16,395,268,2,Amber);
                var contract=GarageAdviceContract();
                Text(panel,contract!=null?"PREPARING FOR":"WORKSHOP TIP",16,406,268,23,15,Amber,true);
                string advice=garageRecommendation!=null?contract.TitleFor(P)+"\nRecommended: "+GarageName(garageRecommendation)
                    :contract!=null?contract.TitleFor(P)+"\nTerrain and positioning still matter.":"Choose a recovery for equipment advice. More power needs matching grip and cable strength.";
                Text(panel,advice,16,433,268,57,16,Paper);
                if(garageRecommendation!=null)
                {
                    var recommend=GarageButton(panel,"RECOMMENDED PART",16,494,268,JumpGarageRecommendation,false,40);
                    GarageFont(recommend.GetComponentInChildren<TMP_Text>(),15);AddGarageHint(recommend,"recommend");
                }
            }
        }
        private void BuildGarageSummary(GaragePart selected)
        {
            var panel=Panel(pageRoot,GaragePreviewX,212,GaragePreviewWidth,144);
            Text(panel,selected==null?SlotName(garageSlot):GarageName(selected),20,12,GaragePreviewWidth-40,40,25,Paper,true);
            string description;
            if(selected==null)description="No matching parts. Switch to All or choose another tire type.";
            else if(RecoveryGarageWorkflow.AppearanceOnly(selected))description="Appearance only · changes the look of your vehicle.";
            else if(garageSlot==GarageSlot.Wheels)
            {
                var before=GaragePerformance.Calculate(P.Build(P.vehicle),session.GarageCatalog);var after=GaragePerformance.Calculate(garageDraft,session.GarageCatalog);
                description="Four-wheel set · mass "+(after.Mass-before.Mass).ToString("+0;-0;0")+" kg · top speed "+((after.Speed-before.Speed)*3.6f).ToString("+0;-0;0")+" km/h.\nMud resistance: "+before.MudResistance.ToString("0.00")+" → "+after.MudResistance.ToString("0.00")+"×. Compare surface grip below.";
            }
            else description=selected.EnglishDescription??"";
            Text(panel,description,20,59,GaragePreviewWidth-40,66,16,Muted);
            var status=Panel(pageRoot,GaragePreviewX,590,GaragePreviewWidth,42);
            bool preview=selected!=null&&RecoveryGarageWorkflow.State(P,selected,session.GarageCatalog)!=GarageItemState.Equipped;
            var replacements=RecoveryGarageWorkflow.ReplacedParts(P.Build(P.vehicle),garageDraft,selected,session.GarageCatalog);
            string caption=preview?"Preview — not fitted":"Fitted setup · saved";
            if(replacements.Count>0)caption+=" · "+replacements.Count+" part(s) will return to Stock";
            else if(!preview&&!string.IsNullOrEmpty(garageMessage))caption=garageMessage;
            Icon(status,preview?RecoveryIcon.Body:RecoveryIcon.Check,12,10,22,preview?Amber:Paper);
            Text(status,caption,44,0,GaragePreviewWidth-56,42,15,preview?Amber:Paper).alignment=TextAlignmentOptions.MidlineLeft;
        }
        private void BuildGarageComparisons(GaragePerformance before,GaragePerformance after)
        {
            if(garageSlot==GarageSlot.Wheels)
            {
                float width=(GaragePreviewWidth-30)/4;
                GarageCompareTile(GaragePreviewX,650,width,"ROAD",before.DryGrip,after.DryGrip,"0.00","×","");
                GarageCompareTile(GaragePreviewX+width+10,650,width,"MUD",before.MudGrip,after.MudGrip,"0.00","×","");
                GarageCompareTile(GaragePreviewX+2*(width+10),650,width,"SNOW",before.SnowGrip,after.SnowGrip,"0.00","×","");
                GarageCompareTile(GaragePreviewX+3*(width+10),650,width,"SAND",before.SandGrip,after.SandGrip,"0.00","×","");
            }
            else
            {
                float width=(GaragePreviewWidth-24)/3;
                if(garageSlot==GarageSlot.Assembly)
                {
                    GarageCompareTile(GaragePreviewX,650,width,"MASS",before.Mass,after.Mass,"0"," kg","Helps anchoring",true);
                    GarageCompareTile(GaragePreviewX+width+12,650,width,"RATED PULL",before.Drive*.004f,after.Drive*.004f,"0.0"," kN","Grip still limits usable pull");
                    GarageCompareTile(GaragePreviewX+2*(width+12),650,width,"TOP SPEED",before.Speed*3.6f,after.Speed*3.6f,"0"," km/h","On firm, flat ground");
                }
                else if(garageSlot==GarageSlot.Chassis)
                {
                    GarageCompareTile(GaragePreviewX,650,width,"TRACK WIDTH",before.Track*100,after.Track*100,"0","%","Wider track helps stability",true);
                    GarageCompareTile(GaragePreviewX+width+12,650,width,"MASS",before.Mass,after.Mass,"0"," kg","Weight affects slopes and grip",true);
                    GarageCompareTile(GaragePreviewX+2*(width+12),650,width,"TOP SPEED",before.Speed*3.6f,after.Speed*3.6f,"0"," km/h","");
                }
                else if(garageSlot==GarageSlot.Winch)
                {
                    GarageCompareTile(GaragePreviewX,650,width,"WINCH FORCE",before.WinchForce/1000,after.WinchForce/1000,"0"," kN","Slows under load");
                    GarageCompareTile(GaragePreviewX+width+12,650,width,"REEL SPEED",before.Winch,after.Winch,"0.0"," m/s","Unloaded reel speed");
                    GarageCompareTile(GaragePreviewX+2*(width+12),650,width,"CABLE LIMIT",before.Cable/1000,after.Cable/1000,"0"," kN","Keep the cable matched");
                }
                else
                {
                    GarageCompareTile(GaragePreviewX,650,width,"RATED PULL",before.Drive*.004f,after.Drive*.004f,"0.0"," kN","Grip still limits usable pull");
                    GarageCompareTile(GaragePreviewX+width+12,650,width,"CABLE LIMIT",before.Cable/1000,after.Cable/1000,"0"," kN","Winch: "+(after.WinchForce/1000).ToString("0")+" kN");
                    if(garageSlot==GarageSlot.Suspension)GarageCompareTile(GaragePreviewX+2*(width+12),650,width,"EXTRA CLEARANCE",before.Lift*100,after.Lift*100,"0"," cm","Helps clear deep ruts");
                    else GarageCompareTile(GaragePreviewX+2*(width+12),650,width,"TOP SPEED",before.Speed*3.6f,after.Speed*3.6f,"0"," km/h","Mass: "+after.Mass.ToString("0")+" kg");
                }
            }
        }
        private void GarageCompareTile(float x,float y,float width,string title,float before,float after,string format,string unit,string note,bool neutral=false)
        {
            var panel=Panel(pageRoot,x,y,width,126);Text(panel,title,12,8,width-24,25,15,Muted,true);
            bool changed=Mathf.Abs(before-after)>.001f;string value=changed?before.ToString(format)+" → "+after.ToString(format)+unit:after.ToString(format)+unit;
            Text(panel,value,12,36,width-24,38,changed?(width<180?16:20):26,changed&&!neutral?(after>before?new Color(.65f,.9f,.58f):Amber):Paper,true);
            string detail=changed?(after-before).ToString("+0.##;-0.##")+unit+" · Fitted → preview":note;
            Text(panel,detail,12,78,width-24,40,15,Muted);
        }
        private void BuildGarageActions(GaragePart selected)
        {
            var primary=GarageButton(pageRoot,GaragePrimaryLabel(selected),GarageCatalogX,792,GarageCatalogWidth,GaragePrimaryAction,true,56);
            primary.name="Garage primary action";primary.interactable=CanFitGaragePart(selected);AddGarageHint(primary,"confirm");
            if(selected!=null&&selected.Price>0&&P.Owns(selected.Id))
            {
                string reason=RecoveryGarageWorkflow.InUseReason(P,selected.Id);
                var sell=GarageButton(pageRoot,reason??("SELL · "+(selected.Price*70/100).ToString("N0")+" CR"),GarageCatalogX,858,GarageCatalogWidth,RequestGarageSale,false,32);
                sell.name="Garage sell action";sell.interactable=reason==null;GarageFont(sell.GetComponentInChildren<TMP_Text>(),15);
                if(reason==null)AddGarageHint(sell,"sell");
            }
        }
        private void BuildGarageFooter()
        {
            var back=GarageButton(pageRoot,session.Training?"FINISH TRAINING":"BACK TO DRIVING",36,792,GarageCategoryWidth,CloseGarage,false,56);AddGarageHint(back,"back");
            Text(pageRoot,"Fitted parts stay saved.",40,852,GarageCategoryWidth-8,40,15,Muted);
            var presets=GarageButton(pageRoot,"SAVED SETUPS",GaragePreviewX,792,GaragePreviewWidth*.53f,()=>OpenGarageOverlay(GarageOverlay.Setups),false,44);AddGarageHint(presets,"setups");
            var reset=GarageButton(pageRoot,"RESET VIEW",GaragePreviewX+GaragePreviewWidth*.53f+12,792,GaragePreviewWidth*.47f-12,ResetGarageView,false,44);AddGarageHint(reset,"reset");
            MenuHint(pageRoot,"nav-primary","Select",GaragePreviewX,846,190);
            MenuHint(pageRoot,"rotate","Rotate",GaragePreviewX+195,846,190);
            if(garageSlot==GarageSlot.Wheels)
            {
                MenuGlyph(pageRoot,"tire-previous",GaragePreviewX+390,846,48);
                MenuGlyph(pageRoot,"tire-next",GaragePreviewX+440,846,48);
                Text(pageRoot,"Tires",GaragePreviewX+496,846,GaragePreviewWidth-496,40,18,Muted).alignment=TextAlignmentOptions.MidlineLeft;
            }
        }
        private void FitNewPanelText()
        {
            foreach(var text in pageRoot.GetComponentsInChildren<TMP_Text>())
            {
                // Canvas layout may already have auto-sized the text. Keep its authored size as the floor.
                if(!textSizes.TryGetValue(text,out float size)||!text.enableAutoSizing)continue;
                if(size*P.textScale>=48){text.rectTransform.anchoredPosition=new Vector2(text.rectTransform.anchoredPosition.x,-50);text.rectTransform.sizeDelta=new Vector2(text.rectTransform.sizeDelta.x,80);}
                text.fontSizeMax=size*P.textScale;text.fontSizeMin=(text.gameObject.name=="Garage item name"?15:size)*Mathf.Min(1,P.textScale);
            }
        }
        private bool UsedPart(string id)=>RecoveryGarageWorkflow.InUseReason(P,id)!=null;
        private bool boardCompleted;
        private int boardAvailableIndex=-1,boardCompletedIndex=-1,boardRenderedCompletedMask;
        private float boardCompletedScrollY;
        private void SelectBoardTab(bool completed)
        {
            if(boardCompleted!=completed)PlayMenuMove();
            RememberBoardScroll();boardCompleted=completed;boardIndex=completed?boardCompletedIndex:boardAvailableIndex;Show("Board");
        }
        private void BuildBoard()
        {
            Header(L("INTERVENȚII DISPONIBILE","AVAILABLE RECOVERIES"),L("Alege intervenția. Pregătește mașina. Adu-i acasă.","Pick a job. Prepare your ride. Bring them home."));Wallet(pageRoot,P.credits);
            var w=session.World;var car=w.Drivers[Mathf.Max(0,session.LocalSlot)];
            var contracts=w.AvailableContracts;boardRenderedCompletedMask=P.completedMask;
            var visible=RecoveryBoardEntries.Select(contracts,P.completedMask,boardCompleted);boardEntries=visible;
            if(!visible.Contains(boardIndex))boardIndex=visible.Count>0?visible[0]:-1;
            if(boardIndex>=0)garagePlannedContractIndex=boardIndex;
            if(boardCompleted)boardCompletedIndex=boardIndex;else boardAvailableIndex=boardIndex;
            var availableTab=Button(pageRoot,L("DISPONIBILE","AVAILABLE"),76,194,284,()=>SelectBoardTab(false),!boardCompleted,30);
            availableTab.gameObject.name="Board available tab";AddBoardHint(availableTab,0,true);
            var completedTab=Button(pageRoot,L("FINALIZATE","COMPLETED"),372,194,284,()=>SelectBoardTab(true),boardCompleted,30);
            completedTab.gameObject.name="Board completed tab";AddBoardHint(completedTab,1,true);
            if(boardCompleted)BuildCompletedRecoveries(visible,contracts,car.transform.position);
            else for(int row=0;row<visible.Count;row++)BuildBoardCard(pageRoot,contracts,visible[row],76,232+row*158,car.transform.position,null);
            if(visible.Count==0)Text(pageRoot,boardCompleted?L("Nicio recuperare finalizată încă.","No completed recoveries yet."):L("Toate recuperările sunt finalizate. Le poți rejuca în Finalizate.","All recoveries are completed. Replay them in Completed."),76,300,580,140,23,Muted,true);
            var detail=Panel(pageRoot,690,232,834,458);detail.name="Recovery details";
            TMP_Text boardDescription = null;
            if(boardIndex>=0)
            {
            var selected=contracts[boardIndex];Text(detail,selected.TitleFor(P),30,17,774,50,33,Paper,true);ContractPhoto(detail,boardIndex,30,80,774,178);
            boardDescription = Text(detail,selected.DescriptionFor(P)+"\n"+L("Teren: ","Terrain: ")+selected.TerrainFor(P),30,274,482,93,18,Paper);
            var catalog = session.GarageCatalog;
            if (catalog != null)
            {
                var equipment = GaragePerformance.Calculate(P.Build(P.vehicle), catalog);
                Text(detail,RecoveryAssistance.Preparation(selected,equipment),30,369,482,40,16,Amber).name="Equipment recommendation";
            }
            bool first=!P.Completed(selected.CompletionId);
            var reward=Panel(detail,542,277,262,111);reward.GetComponentInChildren<RecoveryPlateGraphic>().Style=RecoveryPlateStyle.Paper;Icon(reward,RecoveryIcon.Coins,15,17,26,Ink);Text(reward,first?"REWARD":"REPLAY REWARD",50,12,196,27,16,Ink,true);Text(reward,RecoveryEconomy.BaseOffer(selected,first).ToString("N0")+" CR",17,45,228,50,35,Ink,true).alignment=TextAlignmentOptions.Center;
            string estimate=selected.ExpectedSeconds>0?"~"+Mathf.CeilToInt(selected.ExpectedSeconds/60)+" min  ·  ":"";
            Text(detail,estimate+(first?"First recovery +25%  ·  ":"Replay  ·  ")+"No resets +10%  ·  No breaks +10%",30,411,774,36,16,Muted);
            }
            else Text(detail,boardCompleted?L("Recuperările terminate vor apărea aici pentru rejucare.","Finished recoveries will appear here for replay."):L("Alege o recuperare din Finalizate pentru a o rejuca.","Choose a completed recovery to replay."),30,30,774,100,25,Paper,true);
            Text(pageRoot,visible.Count+L(boardCompleted?" recuperări finalizate":" recuperări disponibile",boardCompleted?" completed recoveries":" available recoveries"),76,708,256,36,16,Paper);
            boardSelectionHint=ControlText(Text(pageRoot,"",340,708,316,36,16,Muted));
            var statusCard=Panel(pageRoot,76,w.Map!=null?750:720,1448,w.Map!=null?40:50);Icon(statusCard,w.HasContract?RecoveryIcon.Hook:RecoveryIcon.Flag,18,13,25,Amber);Text(statusCard,w.HasContract?L("ÎN DESFĂȘURARE: ","IN PROGRESS: ")+w.Contract.TitleFor(P):L("Nicio intervenție activă. Alege unde mergem mai departe.","No active recovery. Choose where we go next."),60,0,1368,50,18,Paper).alignment=TextAlignmentOptions.MidlineLeft;
            startButton=Button(pageRoot,w.HasContract?L("ABANDONEAZĂ INTERVENȚIA","ABANDON RECOVERY"):L("ACCEPTĂ INTERVENȚIA","ACCEPT RECOVERY"),1004,805,520,BoardPrimaryAction,true,62);
            startButton.interactable=CanUseBoardPrimary();AddBoardHint(startButton,2,false);
            var backToDriving=Button(pageRoot,L("ÎNAPOI ÎN JOC","BACK TO DRIVING"),76,805,340,CloseBoard,false,62);
            AddBoardHint(backToDriving,3,false);
            if(!session.IsHost)Text(pageRoot,L("Gazda acceptă intervenția pentru echipă.","The host accepts the recovery for the crew."),690,874,834,24,16,Muted);FitNewPanelText();
            if (boardDescription != null) boardDescription.fontSizeMin = 14f;
            RefreshBoardHints();
        }
        private void ContractPhoto(Transform parent,int index,float x,float y,float width,float height)
        {
            if(contractImages==null||index>=contractImages.Length||contractImages[index]==null)return;
            var image=Node("Recovery photo",parent,x,y,width,height).gameObject.AddComponent<UnityEngine.UI.RawImage>();image.texture=contractImages[index];image.raycastTarget=false;
            float source=image.texture.width/(float)image.texture.height,target=width/height;
            if(target>source){float visible=source/target;image.uvRect=new Rect(0,(1-visible)*.5f,1,visible);}else{float visible=target/source;image.uvRect=new Rect((1-visible)*.5f,0,visible,1);}
        }
    }
}

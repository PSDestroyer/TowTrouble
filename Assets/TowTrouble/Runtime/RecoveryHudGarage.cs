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
        private string SlotName(GarageSlot slot)=>L(new[]{"CAROSERIE","ROȚI","CAPOTĂ","BARĂ FAȚĂ","BARĂ SPATE","ARIPI","ACCESORII","ADMISIE","EVACUARE","ȘASIU","MOTOR","TRANSMISIE","SUSPENSIE","CABLU","TROLIU","VOPSEA"}[(int)slot],new[]{"BODY","WHEELS","BONNET","FRONT BUMPER","REAR BUMPER","FENDERS","ACCESSORIES","INTAKE","EXHAUST","CHASSIS","ENGINE","TRANSMISSION","SUSPENSION","CABLE","WINCH","PAINT"}[(int)slot]);
        private void PrepareGarage()
        {garageDraft=P.Build(P.vehicle).Copy();garageDraft.Normalize(session.GarageCatalog,P.Owns);garageSelected=garageDraft.Item(garageSlot,session.GarageCatalog);garageMessage="";garageScrollY=0;}
        private int garageSection=1;
        private void BuildGarage()
        {
            var catalog=session.GarageCatalog;if(catalog==null){Header(L("GARAJ INDISPONIBIL","GARAGE UNAVAILABLE"),"");return;}
            if(garageDraft==null)PrepareGarage();
            Header(L("ATELIERUL TĂU","YOUR WORKSHOP"),L("Pregătește mașina pentru următoarea recuperare.","Get your ride ready for the next recovery."));Wallet(pageRoot,P.credits);
            var categories=Panel(pageRoot,36,212,GarageCategoryWidth,548);Text(categories,L("MODIFICĂRI","CUSTOMIZE"),16,14,268,28,19,Paper,true);
            string[] sections={L("STIL","STYLE"),L("TEHNIC","TECH"),L("TRACTARE","TOWING")};
            for(int i=0;i<3;i++){int section=i;var tab=Button(categories,sections[i],new[]{12f,92f,188f}[i],51,new[]{72f,88f,100f}[i],()=>{garageSection=section;garageScrollY=0;Show("Garage");},garageSection==i,34);var text=tab.GetComponentInChildren<TMP_Text>();text.fontSize=15*P.textScale;text.textWrappingMode=TextWrappingModes.NoWrap;}
            GarageSlot[] slots=garageSection==0?new[]{GarageSlot.Assembly,GarageSlot.Bonnet,GarageSlot.FrontBumper,GarageSlot.RearBumper,GarageSlot.Fenders,GarageSlot.Roof,GarageSlot.Intake,GarageSlot.Exhaust,GarageSlot.Paint}:garageSection==1?new[]{GarageSlot.Wheels,GarageSlot.Engine,GarageSlot.Transmission,GarageSlot.Suspension,GarageSlot.Chassis}:new[]{GarageSlot.Cable,GarageSlot.Winch};
            if(Array.IndexOf(slots,garageSlot)<0){garageSlot=slots[0];garageSelected=garageDraft.Item(garageSlot,catalog);garageScrollY=0;}
            for(int i=0;i<slots.Length;i++)
            {var choice=slots[i];var tab=Button(categories,SlotName(choice),12,101+i*46,GarageCategoryWidth-24,()=>{garageSlot=choice;garageScrollY=0;garageSelected=garageDraft.Item(choice,catalog);Show("Garage");},garageSlot==choice,42);ButtonIcon(tab,SlotIcon(choice),garageSlot==choice);tab.GetComponentInChildren<TMP_Text>().fontSize=16*P.textScale;}
            if(slots.Length<=5){Icon(categories,garageSection==1?RecoveryIcon.Gears:RecoveryIcon.Hook,16,411,23,Amber);Text(categories,L("SFAT DE ATELIER","WORKSHOP TIP"),48,409,236,30,14,Paper,true);Panel(categories,16,445,268,2,Amber);Text(categories,garageSection==1?L("Mai multă putere ajută doar dacă ai aderență.","More power helps when your tires can grip."):L("Trage lin. Poziționează mașina înainte să forțezi cablul.","Pull steadily. Position your car before loading the cable."),16,460,268,69,14,Muted);}
            session.FocusGarage(garageSlot,garageSelected);
            var selected=catalog.Find(garageSelected);var summary=Panel(pageRoot,GaragePreviewX,212,GaragePreviewWidth,garageSlot==GarageSlot.Wheels?164:136);
            Text(summary,selected==null?SlotName(garageSlot):PartName(selected),22,12,GaragePreviewWidth-44,43,27,Paper,true);
            Text(summary,selected==null?"":L(selected.Description??"",selected.EnglishDescription??""),22,58,GaragePreviewWidth-44,garageSlot==GarageSlot.Wheels?88:60,17,Muted);
            var candidates=new List<GaragePart>();foreach(var part in catalog.Parts)if(part.Slot==garageSlot&&catalog.Compatible(part,garageDraft))candidates.Add(part);
            candidates.Sort((a,b)=>
            {
                int price=RecoveryEconomy.Price(P,a).CompareTo(RecoveryEconomy.Price(P,b));
                if(price!=0)return price;
                int name=string.Compare(PartName(a),PartName(b),StringComparison.Ordinal);
                return name!=0?name:string.CompareOrdinal(a.Id,b.Id);
            });
            BuildGarageCatalog(candidates);
            var stats=GaragePerformance.Calculate(garageDraft,catalog);var original=GaragePerformance.Calculate(P.Build(P.vehicle),catalog);
            float tileWidth=(GaragePreviewWidth-24)/3;
            StatTile(GaragePreviewX,650,tileWidth,RecoveryIcon.Wheel,L("FORȚĂ","FORCE"),(stats.Drive*4/1000).ToString("0.0")+" kN",L("Noroi: ","Mud: ")+stats.MudGrip.ToString("0.00")+"×"+(Mathf.Abs(stats.Drive-original.Drive)>1?"  ·  "+((stats.Drive-original.Drive)*4/1000).ToString("+0.0;-0.0")+" kN":""));
            StatTile(GaragePreviewX+tileWidth+12,650,tileWidth,RecoveryIcon.Hook,L("CABLU","CABLE"),(stats.Cable/1000).ToString("0")+" kN",L("Troliu: ","Winch: ")+(stats.WinchForce/1000).ToString("0")+" kN · "+stats.Winch.ToString("0.0")+" m/s");
            StatTile(GaragePreviewX+(tileWidth+12)*2,650,tileWidth,RecoveryIcon.Body,L("VEHICUL","VEHICLE"),stats.Mass.ToString("0")+" kg",L("Viteză: ","Top speed: ")+(stats.Speed*3.6f).ToString("0")+" km/h");
            if(!string.IsNullOrEmpty(garageMessage)){var message=Panel(pageRoot,GaragePreviewX,592,GaragePreviewWidth,40);Icon(message,RecoveryIcon.Check,12,10,20,Amber);Text(message,garageMessage,44,0,GaragePreviewWidth-56,40,16,Paper);}
            if(selected!=null)
            {
                bool owned=selected.Price==0||P.Owns(selected.Id);
                var equip=Button(pageRoot,owned?L("MONTEAZĂ CONFIGURAȚIA","FIT SETUP"):L("CUMPĂRĂ ȘI MONTEAZĂ","BUY AND FIT")+" · "+RecoveryEconomy.Price(P,selected)+" CR",GarageCatalogX,792,GarageCatalogWidth,()=>
                {if(!owned&&!RecoveryEconomy.Buy(P,selected))return;garageDraft.Normalize(catalog,P.Owns);session.CommitGarage(garageDraft);garageMessage=L("Configurație montată și salvată.","Setup fitted and saved.");Show("Garage");},true,56);equip.interactable=owned||P.credits>=RecoveryEconomy.Price(P,selected);
                var sell=Button(pageRoot,L("VINDE · ","SELL · ")+(selected.Price*70/100)+" CR",GarageCatalogX,858,GarageCatalogWidth,()=>{if(RecoveryEconomy.Sell(P,selected,catalog)){P.Save();garageSelected=null;garageDraft=P.Build(P.vehicle).Copy();garageMessage=L("Piesă vândută.","Part sold.");Show("Garage");}},false,32);sell.interactable=selected.Price>0&&P.Owns(selected.Id)&&!UsedPart(selected.Id);sell.GetComponentInChildren<TMP_Text>().fontSize=15*P.textScale;
            }
            string[] names={L("Pădure","Forest"),L("Munte","Mountain"),L("Nisip","Sand")};var icons=new[]{RecoveryIcon.Tree,RecoveryIcon.Mountain,RecoveryIcon.Sand};
            for(int i=0;i<3;i++)
            {
                int preset=i;string key="setup_"+P.vehicle+"_"+i;var saved=P.presets.Find(part=>part.Name==key);var card=Panel(pageRoot,GaragePreviewX+i*(tileWidth+12),792,tileWidth,98);Icon(card,icons[i],14,12,22,Amber);Text(card,names[i],40,6,tileWidth-48,32,16,Paper,true);
                var save=Button(card,L("SALVEAZĂ","SAVE"),10,48,(tileWidth-28)/2,()=>{if(saved==null){saved=new GaragePreset{Name=key};P.presets.Add(saved);}saved.Build=P.Build(P.vehicle).Copy();P.Save();garageMessage=L("Configurație memorată.","Preset saved.");Show("Garage");},false,34);save.gameObject.name=key+"_save";save.GetComponentInChildren<TMP_Text>().fontSize=12*P.textScale;
                var load=Button(card,L("APLICĂ","APPLY"),18+(tileWidth-28)/2,48,(tileWidth-28)/2,()=>{garageDraft=saved.Build.Copy();garageDraft.Normalize(catalog,P.Owns);session.CommitGarage(garageDraft);session.PreviewGarage(garageDraft);garageSelected=garageDraft.Item(garageSlot,catalog);Show("Garage");},false,34);load.gameObject.name=key+"_load";load.interactable=saved?.Build!=null;load.GetComponentInChildren<TMP_Text>().fontSize=12*P.textScale;
            }
            var back=Button(pageRoot,L("ÎNAPOI ÎN JOC","BACK TO DRIVING"),36,792,GarageCategoryWidth,()=>{garageDraft=null;session.CloseOverlay();Show("Gameplay");},false,56);ButtonIcon(back,RecoveryIcon.Back);back.GetComponentInChildren<TMP_Text>().fontSize=18*P.textScale;
            Text(pageRoot,L("Montarea pieselor deținute este gratuită.","Refitting owned equipment is free."),40,858,GarageCategoryWidth-8,38,13,Muted);FitNewPanelText();
        }
        private void StatTile(float x,float y,float width,RecoveryIcon kind,string label,string value,string note)
        {
            var tile=Panel(pageRoot,x,y,width,110);Icon(tile,kind,13,14,23,Amber);Text(tile,label,45,8,width-59,37,14,Muted,true);Text(tile,value,16,43,width-32,40,30,Paper,true);Text(tile,note,16,83,width-32,23,13,Muted);
        }
        private void FitNewPanelText()
        {
            foreach(var text in pageRoot.GetComponentsInChildren<TMP_Text>())
            {
                if(text.fontSize>=48){text.rectTransform.anchoredPosition=new Vector2(text.rectTransform.anchoredPosition.x,-50);text.rectTransform.sizeDelta=new Vector2(text.rectTransform.sizeDelta.x,80);}
                text.enableAutoSizing=true;text.fontSizeMax=text.fontSize;text.fontSizeMin=text.fontSize/Mathf.Max(1,P.textScale);
            }
        }
        private bool UsedPart(string id)
        {foreach(var b in P.builds)if(b.Items!=null&&Array.IndexOf(b.Items,id)>=0)return true;foreach(var p in P.presets)if(p.Build?.Items!=null&&Array.IndexOf(p.Build.Items,id)>=0)return true;return false;}
        private int boardPage,boardBiome=-1;
        private void BuildBoard()
        {
            Header(L("INTERVENȚII DISPONIBILE","AVAILABLE RECOVERIES"),L("Alege intervenția. Pregătește mașina. Adu-i acasă.","Pick a job. Prepare your ride. Bring them home."));Wallet(pageRoot,P.credits);
            var w=session.World;var car=w.Drivers[Mathf.Max(0,session.LocalSlot)];
            var contracts=w.AvailableContracts;var visible=new List<int>();
            for(int i=0;i<contracts.Length;i++)if(w.Map==null||boardBiome<0||contracts[i].Zone==boardBiome)visible.Add(i);
            if(w.Map!=null)
            {
                string[] tabs={L("TOATE","ALL"),L("PĂDURE","FOREST"),L("LAC","LAKE"),L("MUNTE","MOUNTAIN"),L("ZĂPADĂ","SNOW"),L("PLAJĂ","BEACH"),L("DEȘERT","DESERT")};
                for(int i=0;i<7;i++){int filter=i-1;Button(pageRoot,tabs[i],76+i*207,194,196,()=>{boardBiome=filter;boardPage=0;boardIndex=0;for(int j=0;j<contracts.Length;j++)if(filter<0||contracts[j].Zone==filter){boardIndex=j;break;}Show("Board");},boardBiome==filter,30);}
            }
            int pages=Mathf.Max(1,(visible.Count+2)/3);boardPage=Mathf.Clamp(boardPage,0,pages-1);boardIndex=Mathf.Clamp(boardIndex,0,contracts.Length-1);
            for(int row=0;row<3;row++)
            {
                int index=boardPage*3+row;if(index>=visible.Count)break;int id=visible[index];var contract=contracts[id];bool active=boardIndex==id;int i=row;float distance=Vector3.Distance(car.transform.position,contract.Target);
                var card=Button(pageRoot,contract.TitleFor(P),76,232+i*158,580,()=>{boardIndex=id;Show("Board");},active,142);var title=card.GetComponentInChildren<TMP_Text>();title.rectTransform.anchoredPosition=new Vector2(216,-12);title.rectTransform.sizeDelta=new Vector2(344,50);title.font=displayFont!=null?displayFont:font;title.fontSize=24*P.textScale;
                ContractPhoto(card.transform,id,14,14,184,112);var number=Panel(card.transform,23,22,38,28,Amber);Text(number,contract.Id.ToString("00"),0,0,38,28,16,Ink,true).alignment=TextAlignmentOptions.Center;
                Text(card.transform,contract.Difficulty(P)+"  ·  "+Mathf.RoundToInt(distance)+" m",218,65,340,25,16,active?Ink:Muted);Icon(card.transform,RecoveryIcon.Coins,218,103,22,active?Ink:Amber);Text(card.transform,contract.Reward.ToString("N0")+" CR",250,94,300,36,23,active?Ink:Amber,true);
            }
            var selected=contracts[boardIndex];var detail=Panel(pageRoot,690,232,834,458);Text(detail,selected.TitleFor(P),30,17,774,50,33,Paper,true);ContractPhoto(detail,boardIndex,30,80,774,178);
            Text(detail,selected.DescriptionFor(P)+"\n"+L("Teren: ","Terrain: ")+selected.TerrainFor(P)+"\n"+(selected.IsOpenWorld?RecoveryMap.Preparation(selected.Zone,P.language=="en"):L(new[]{"Echipamentul inițial este suficient.","Caută sprijin pe teren tare și trage spre drum.","Anvelope de noroi. Accelerează treptat."}[boardIndex],new[]{"Stock equipment is enough.","Find firm ground and pull towards the road.","Mud tires. Build throttle gently."}[boardIndex])),30,277,482,115,19,Paper);
            var reward=Panel(detail,542,277,262,111);reward.GetComponentInChildren<RecoveryPlateGraphic>().Style=RecoveryPlateStyle.Paper;Icon(reward,RecoveryIcon.Coins,15,17,26,Ink);Text(reward,L("RECOMPENSĂ","REWARD"),50,12,196,27,14,Ink,true);Text(reward,selected.Reward.ToString("N0")+" CR",17,45,228,50,35,Ink,true).alignment=TextAlignmentOptions.Center;
            Text(detail,L("BONUSURI  ·  +10% fără resetări  ·  +10% fără cablu rupt  ·  +5% prima recuperare","BONUSES  ·  +10% no resets  ·  +10% no cable breaks  ·  +5% first recovery"),30,411,774,30,14,Muted);
            if(w.Map!=null){Button(pageRoot,"‹",76,704,90,()=>{boardPage--;boardIndex=visible[Mathf.Max(0,boardPage*3)];Show("Board");},false,34).interactable=boardPage>0;Text(pageRoot,(boardPage+1)+" / "+pages+"   ·   "+visible.Count+L(" recuperări"," recoveries"),180,708,370,30,16,Paper);Button(pageRoot,"›",566,704,90,()=>{boardPage++;boardIndex=visible[Mathf.Min(visible.Count-1,boardPage*3)];Show("Board");},false,34).interactable=boardPage<pages-1;}
            var statusCard=Panel(pageRoot,76,w.Map!=null?750:720,1448,w.Map!=null?40:50);Icon(statusCard,w.HasContract?RecoveryIcon.Hook:RecoveryIcon.Flag,18,13,25,Amber);Text(statusCard,w.HasContract?L("ÎN DESFĂȘURARE: ","IN PROGRESS: ")+w.Contract.TitleFor(P):L("Nicio intervenție activă. Alege unde mergem mai departe.","No active recovery. Choose where we go next."),60,0,1368,50,18,Paper).alignment=TextAlignmentOptions.MidlineLeft;
            startButton=Button(pageRoot,w.HasContract?L("AI DEJA UN CONTRACT","JOB ALREADY ACTIVE"):L("ACCEPTĂ INTERVENȚIA","ACCEPT RECOVERY"),1004,805,520,()=>session.AcceptBoardContract(boardIndex),true,62);startButton.interactable=session.IsHost&&w.Phase==RecoveryPhase.Explore&&!w.CrewInGarage;
            if(w.HasContract)Button(pageRoot,L("ABANDONEAZĂ","ABANDON"),690,805,290,()=>Confirm(L("Abandonezi intervenția fără recompensă?","Abandon this job without payment?"),()=>{session.FinishExplorationContract();Show("Gameplay");},"Board"),false,62).interactable=session.IsHost;
            Button(pageRoot,L("ÎNAPOI ÎN JOC","BACK TO DRIVING"),76,805,340,()=>{session.CloseOverlay();Show("Gameplay");},false,62);
            if(!session.IsHost)Text(pageRoot,L("Gazda acceptă intervenția pentru echipă.","The host accepts the recovery for the crew."),690,874,834,24,14,Muted);FitNewPanelText();
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

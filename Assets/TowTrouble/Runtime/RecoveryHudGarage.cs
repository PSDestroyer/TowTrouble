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
        private int garagePage,boardIndex;
        private TMP_Text garagePrompt;
        private string SlotName(GarageSlot slot)=>L(new[]{"CAROSERIE","ROȚI","CAPOTĂ","BARĂ FAȚĂ","BARĂ SPATE","ARIPI","ACCESORII","ADMISIE","EVACUARE","ȘASIU","MOTOR","TRANSMISIE","SUSPENSIE","CABLU","TROLIU","VOPSEA"}[(int)slot],new[]{"BODY","WHEELS","BONNET","FRONT BUMPER","REAR BUMPER","FENDERS","ACCESSORIES","INTAKE","EXHAUST","CHASSIS","ENGINE","TRANSMISSION","SUSPENSION","CABLE","WINCH","PAINT"}[(int)slot]);
        private void PrepareGarage()
        {garageDraft=P.Build(P.vehicle).Copy();garageDraft.Normalize(session.GarageCatalog,P.Owns);garageSelected=garageDraft.Item(garageSlot,session.GarageCatalog);garageMessage="";garagePage=0;}
        private void BuildGarage()
        {
            var catalog=session.GarageCatalog;if(catalog==null){Header(L("GARAJ INDISPONIBIL","GARAGE UNAVAILABLE"),"");return;}
            if(garageDraft==null)PrepareGarage();
            Panel(pageRoot,0,0,1600,185,new Color(.035f,.045f,.04f,.6f));
            Panel(pageRoot,306,205,690,150);
            Header(L("ATELIERUL TĂU","YOUR WORKSHOP"),L("Previzualizează piesele. Montează ce deții. Salvează configurația.","Preview parts. Fit owned equipment. Save your setup."));
            Text(pageRoot,P.credits.ToString("N0")+" CR",1130,70,390,55,32,Amber,true).alignment=TextAlignmentOptions.Right;
            var categories=Panel(pageRoot,36,205,246,550);
            foreach(GarageSlot slot in Enum.GetValues(typeof(GarageSlot)))
            {var choice=slot;var b=Button(categories,SlotName(slot),10,8+(int)slot*33,226,()=>{garageSlot=choice;garagePage=0;garageSelected=garageDraft.Item(choice,catalog);Show("Garage");},garageSlot==slot,30);b.GetComponentInChildren<TMP_Text>().fontSize=15*P.textScale;}
            var candidates=new List<GaragePart>();foreach(var p in catalog.Parts)if(p.Slot==garageSlot&&catalog.Compatible(p,garageDraft))candidates.Add(p);
            candidates.Sort((a,b)=>{int Group(GaragePart p)=>p.Price==0?0:p.Id.StartsWith("part_")?2:1;int order=Group(a).CompareTo(Group(b));return order!=0?order:string.Compare(a.Label(P),b.Label(P),StringComparison.Ordinal);});
            int pages=Mathf.Max(1,(candidates.Count+5)/6);garagePage=Mathf.Clamp(garagePage,0,pages-1);
            var list=Panel(pageRoot,1020,205,540,555);
            Text(list,SlotName(garageSlot)+"   "+candidates.Count,20,15,500,40,21,Amber,true);
            for(int i=0;i<6;i++)
            {
                int index=garagePage*6+i;if(index>=candidates.Count)break;var part=candidates[index];bool owned=part.Price==0||P.Owns(part.Id);
                string label=part.Label(P)+"\n"+(owned?L("DEȚINUT","OWNED"):RecoveryEconomy.Price(P,part).ToString("N0")+" CR");
                var b=Button(list,label,16,60+i*61,508,()=>{garageSelected=part.Id;garageDraft.Set(part,catalog);garageDraft.Normalize(catalog);session.PreviewGarage(garageDraft);Show("Garage");},garageSelected==part.Id,56);b.GetComponentInChildren<TMP_Text>().fontSize=17*P.textScale;
            }
            Button(list,"‹",16,438,90,()=>{garagePage--;Show("Garage");});Text(list,(garagePage+1)+" / "+pages,126,448,270,35,20,Paper).alignment=TextAlignmentOptions.Center;Button(list,"›",418,438,106,()=>{garagePage++;Show("Garage");});
            var selected=catalog.Find(garageSelected);var installed=P.Build(P.vehicle);var stats=GaragePerformance.Calculate(garageDraft,catalog);var original=GaragePerformance.Calculate(installed,catalog);
            var performance=Panel(pageRoot,306,624,690,136);
            Text(performance,L("CONFIGURAȚIA PREVIZUALIZATĂ","PREVIEW SETUP"),18,12,654,26,17,Amber,true);
            Text(performance,L("Forță la roți: ","Wheel force: ")+(original.Drive*4/1000).ToString("0.0")+" → "+(stats.Drive*4/1000).ToString("0.0")+" kN   •   "+L("Masă: ","Mass: ")+stats.Mass.ToString("0")+" kg\n"+L("Cablu: ","Cable: ")+(stats.Cable/1000).ToString("0")+" kN   •   "+L("Aderență noroi: ","Mud grip: ")+stats.MudGrip.ToString("0.00")+"×\n"+L("Viteză maximă: ","Top speed: ")+(stats.Speed*3.6f).ToString("0")+" km/h",18,48,654,86,19,Paper);
            if(selected!=null)
            {
                Text(pageRoot,selected.Label(P),310,205,680,55,27,Paper,true);
                Text(pageRoot,L(selected.Description??"",selected.EnglishDescription??""),310,266,650,90,18,Muted);
                bool owned=selected.Price==0||P.Owns(selected.Id);
                var equip=Button(pageRoot,owned?L("MONTEAZĂ CONFIGURAȚIA","FIT SETUP"):L("CUMPĂRĂ ȘI MONTEAZĂ","BUY AND FIT")+" · "+RecoveryEconomy.Price(P,selected)+" CR",1020,778,540,()=>
                {
                    if(!owned&&!RecoveryEconomy.Buy(P,selected))return;
                    garageDraft.Normalize(catalog,P.Owns);session.CommitGarage(garageDraft);garageMessage=L("Configurație montată și salvată.","Setup fitted and saved.");Show("Garage");
                },true,56);
                equip.interactable=owned||P.credits>=RecoveryEconomy.Price(P,selected);
                Button(pageRoot,L("VINDE · ","SELL · ")+(selected.Price*70/100)+" CR",1020,842,300,()=>{if(RecoveryEconomy.Sell(P,selected,catalog)){P.Save();garageSelected=null;garageDraft=P.Build(P.vehicle).Copy();garageMessage=L("Piesă vândută.","Part sold.");Show("Garage");}},false,40).interactable=selected.Price>0&&P.Owns(selected.Id)&&!UsedPart(selected.Id);
            }
            Text(pageRoot,garageMessage,310,570,680,45,19,Amber,true);
            string[] names={L("Pădure","Forest"),L("Munte","Mountain"),L("Nisip","Sand")};
            for(int i=0;i<3;i++)
            {
                int preset=i;float x=306+i*232;string key="setup_"+P.vehicle+"_"+i;var saved=P.presets.Find(p=>p.Name==key);
                Button(pageRoot,names[i]+" · "+L("Salvează","Save"),x,778,220,()=>{if(saved==null){saved=new GaragePreset{Name=key};P.presets.Add(saved);}saved.Build=P.Build(P.vehicle).Copy();P.Save();garageMessage=L("Configurație memorată.","Preset saved.");Show("Garage");},false,42).GetComponentInChildren<TMP_Text>().fontSize=16*P.textScale;
                Button(pageRoot,L("Încarcă","Load"),x,830,220,()=>{garageDraft=saved.Build.Copy();garageDraft.Normalize(catalog,P.Owns);session.CommitGarage(garageDraft);session.PreviewGarage(garageDraft);garageSelected=garageDraft.Item(garageSlot,catalog);Show("Garage");},false,42).interactable=saved?.Build!=null;
            }
            Button(pageRoot,L("IEȘI DIN GARAJ","LEAVE GARAGE"),36,778,246,()=>{garageDraft=null;session.CloseOverlay();Show("Gameplay");},false,56);
            Text(pageRoot,L("Montarea pieselor deținute este gratuită.","Refitting owned equipment is free."),36,842,246,55,14,Muted);
            FitNewPanelText();
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
        private void BuildBoard()
        {
            Header(L("INTERVENȚII DISPONIBILE","AVAILABLE RECOVERIES"),L("Alege intervenția și condu până la vehiculul blocat.","Choose a job and drive to the stranded vehicle."));
            Text(pageRoot,P.credits.ToString("N0")+" CR",1140,70,384,55,32,Amber,true).alignment=TextAlignmentOptions.Right;
            var w=session.World;var car=w.Drivers[Mathf.Max(0,session.LocalSlot)];
            for(int i=0;i<RecoveryContract.All.Length;i++)
            {
                int id=i;var c=RecoveryContract.Get(i);float distance=Vector3.Distance(car.transform.position,c.Target);
                string label=c.Id.ToString("00")+"  "+c.TitleFor(P)+"\n"+c.Difficulty(P)+"   •   "+Mathf.RoundToInt(distance)+" m   •   "+c.Reward+" CR";
                var b=Button(pageRoot,label,76,232+i*116,580,()=>{boardIndex=id;Show("Board");},boardIndex==i,98);b.GetComponentInChildren<TMP_Text>().fontSize=22*P.textScale;
            }
            var selected=RecoveryContract.Get(boardIndex);var card=Panel(pageRoot,690,232,834,480);
            Text(card,selected.TitleFor(P),30,25,774,55,33,Paper,true);
            Text(card,selected.DescriptionFor(P)+"\n\n"+L("Teren: ","Terrain: ")+selected.TerrainFor(P)+"\n"+L("Pregătire: ","Equipment: ")+L(new[]{"Echipamentul inițial este suficient.","Tracțiune și poziționare pe teren tare.","Anvelope de noroi; accelerație controlată."}[boardIndex],new[]{"Stock equipment is enough.","Traction and positioning on firm ground.","Mud tires; controlled throttle."}[boardIndex]),30,102,774,200,23,Muted);
            Text(card,L("RECOMPENSĂ: ","REWARD: ")+selected.Reward+" CR\n"+L("+10% fără resetări · +10% fără cablu rupt · +5% prima finalizare","+10% no resets · +10% no cable breaks · +5% first completion"),30,328,774,100,21,Amber,true);
            Text(pageRoot,w.HasContract?L("CONTRACT ACTIV: ","ACTIVE JOB: ")+w.Contract.TitleFor(P):L("Niciun contract activ. Mașina ta rămâne în locul în care ai oprit.","No active job. Your car stays where you stopped."),76,665,580,100,20,Paper);
            startButton=Button(pageRoot,w.HasContract?L("AI DEJA UN CONTRACT","JOB ALREADY ACTIVE"):L("ACCEPTĂ CONTRACTUL","ACCEPT JOB"),1004,778,520,()=>session.AcceptBoardContract(boardIndex),true);startButton.interactable=session.IsHost&&w.Phase==RecoveryPhase.Explore&&!w.CrewInGarage;
            if(w.HasContract)Button(pageRoot,L("ABANDONEAZĂ CONTRACTUL","ABANDON JOB"),690,778,290,()=>Confirm(L("Abandonezi intervenția fără recompensă?","Abandon this job without payment?"),()=>{session.FinishExplorationContract();Show("Gameplay");},"Board")).interactable=session.IsHost;
            Button(pageRoot,L("ÎNAPOI ÎN JOC","BACK TO DRIVING"),76,808,340,()=>{session.CloseOverlay();Show("Gameplay");});
            if(!session.IsHost)Text(pageRoot,L("Gazda acceptă intervenția pentru echipă.","The host accepts the job for the crew."),690,846,834,35,18,Muted);
            FitNewPanelText();
        }
    }
}

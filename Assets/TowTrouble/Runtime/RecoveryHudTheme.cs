using TMPro;
using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        [SerializeField] private TMP_FontAsset displayFont;
        public void ConfigureTypography(TMP_FontAsset body,TMP_FontAsset display){font=body;displayFont=display;}
        private RecoveryPlateGraphic Plate(RectTransform parent,Color tint,bool primary=false)
        {
            var art=Stretch("Sign plate",parent);var plate=art.gameObject.AddComponent<RecoveryPlateGraphic>();plate.raycastTarget=false;
            bool shade=parent.sizeDelta.x>=1500;
            plate.Style=shade?RecoveryPlateStyle.Shade:primary?RecoveryPlateStyle.Gold:RecoveryPlateStyle.Charcoal;
            if(!shade&&!primary&&((tint.b>tint.r*1.3f)||(tint.r>.65f&&tint.g<.5f))){plate.Style=RecoveryPlateStyle.Custom;plate.Tint=tint;}
            plate.Opacity=shade?Mathf.Clamp01(tint.a/.7f):Mathf.Max(.84f,tint.a);plate.color=Color.white;return plate;
        }
        private RecoveryIconGraphic Icon(Transform parent,RecoveryIcon kind,float x,float y,float size,Color tint)
        {var go=Node(kind+" icon",parent,x,y,size,size);var icon=go.gameObject.AddComponent<RecoveryIconGraphic>();icon.Kind=kind;icon.color=tint;icon.raycastTarget=false;return icon;}
        private void ButtonIcon(UnityEngine.UI.Button button,RecoveryIcon kind,bool primary=false)
        {
            var rt=(RectTransform)button.transform;var text=button.GetComponentInChildren<TMP_Text>();
            float size=Mathf.Min(28,rt.sizeDelta.y*.48f);Icon(rt,kind,16,(rt.sizeDelta.y-size)*.5f,size,primary?Ink:Paper);
            text.rectTransform.anchoredPosition=new Vector2(54,0);text.rectTransform.sizeDelta=new Vector2(rt.sizeDelta.x-72,rt.sizeDelta.y);
        }
        private RecoveryIcon? ActionIcon(string label)
        {
            string s=label.ToUpperInvariant();
            if(s.Contains("EXPLORE")||s.Contains("EXPLOREAZ")||s.StartsWith("CONTINU")||s.StartsWith("RESUME"))return RecoveryIcon.Play;
            if(s.StartsWith("TUTORIAL")||s.Contains("CONTRACT")||s=="INTERVENȚII"||s=="RECOVERIES")return RecoveryIcon.Flag;
            if(s.Contains("ÎN DOI")||s.Contains("CO-OP")||s.Contains("READY")||s.Contains("PREGĂTIT"))return RecoveryIcon.Crew;
            if(s=="VEHICULE"||s=="VEHICLES")return RecoveryIcon.Body;
            if(s=="SETĂRI"||s=="SETTINGS")return RecoveryIcon.Settings;
            if(s.StartsWith("CUMPĂRĂ")||s.StartsWith("MONTEAZĂ")||s.StartsWith("BUY AND")||s.StartsWith("FIT SETUP"))return RecoveryIcon.Gears;
            if(s.StartsWith("VINDE")||s.StartsWith("SELL"))return RecoveryIcon.Coins;
            if(s.StartsWith("ÎNAPOI")||s.StartsWith("BACK")||s.StartsWith("IEȘI")||s.StartsWith("LEAVE")||s.StartsWith("PĂRĂSEȘTE"))return RecoveryIcon.Back;
            return null;
        }
        private RecoveryIcon SlotIcon(GarageSlot slot)
        {
            switch(slot){case GarageSlot.Assembly:return RecoveryIcon.Body;case GarageSlot.Wheels:return RecoveryIcon.Wheel;case GarageSlot.Bonnet:return RecoveryIcon.Bonnet;case GarageSlot.FrontBumper:case GarageSlot.RearBumper:case GarageSlot.Fenders:return RecoveryIcon.Bumper;case GarageSlot.Roof:return RecoveryIcon.Roof;case GarageSlot.Intake:case GarageSlot.Engine:return RecoveryIcon.Engine;case GarageSlot.Exhaust:return RecoveryIcon.Exhaust;case GarageSlot.Chassis:return RecoveryIcon.Chassis;case GarageSlot.Transmission:return RecoveryIcon.Gears;case GarageSlot.Suspension:return RecoveryIcon.Spring;case GarageSlot.Cable:return RecoveryIcon.Hook;case GarageSlot.Winch:return RecoveryIcon.Winch;default:return RecoveryIcon.Paint;}
        }
        private void Wallet(Transform parent,int amount,float x=1240,float y=48,float width=284)
        {
            var badge=Panel(parent,x,y,width,62);Icon(badge,RecoveryIcon.Coins,15,15,30,Amber);
            var text=Text(badge,amount.ToString("N0")+" CR",54,0,width-70,62,25,Paper,true);text.alignment=TextAlignmentOptions.MidlineRight;text.enableAutoSizing=true;text.fontSizeMin=18;text.fontSizeMax=25*P.textScale;
        }
        private string PartName(GaragePart part)
        {
            string name=part.Label(P);foreach(string prefix in new[]{"Attachment Top ","Motor Exhaust ","Motor Carb ","Chassis ","Fender ","Bumper ","Bonnet ","Wheel ","Body ","Historic ","Rod "})if(name.StartsWith(prefix)){name=name.Substring(prefix.Length);break;}
            return name;
        }
    }
}

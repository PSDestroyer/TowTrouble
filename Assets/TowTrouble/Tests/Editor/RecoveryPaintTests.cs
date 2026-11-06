using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryPaintTests
    {
        private RecoveryGarageCatalog Catalog=>AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");
        private static IEnumerable<TestCaseData> Starters(){for(int model=0;model<4;model++)for(int paint=0;paint<4;paint++)yield return new TestCaseData(model,paint);}
        [TestCaseSource(nameof(Starters))] public void StarterFinishIsFreeFittedOwnedAndPersistent(int model,int paint)
        {
            var p=new RecoveryProfile();Assert.That(RecoveryEconomy.ChooseStarter(p,model,paint),Is.True);Assert.That(p.credits,Is.Zero);
            string id=paint==0?"stock_Paint":"paint_"+paint;Assert.That(p.Build(model).Item(GarageSlot.Paint,Catalog),Is.EqualTo(id));
            Assert.That(paint==0||p.Owns(id),Is.True);var copy=RecoveryProfile.Parse(JsonUtility.ToJson(p));copy.Build(model).Normalize(Catalog,copy.Owns);
            Assert.That(RecoveryPaintPalette.Resolve(copy.Build(model),Catalog),Is.EqualTo(paint));Assert.That(copy.ownedVehicles,Is.EqualTo(1<<model));
            Assert.That(RecoveryEconomy.ChooseStarter(copy,(model+1)%4,(paint+1)%4),Is.False);
        }
        [Test] public void LegacyGlobalFinishIsRecoveredOnceWithoutChangingMoneyOrProgress()
        {
            var p=new RecoveryProfile{starterChosen=true,starterVehicle=1,vehicle=1,ownedVehicles=2,paint=3,credits=4321,completedMask=7};p.Build(1).Items=new[]{"stock_Paint","engine_2"};p.ownedParts.Add("engine_2");
            var copy=RecoveryProfile.Parse(JsonUtility.ToJson(p));Assert.That(copy.Build(1).Items,Does.Contain("paint_3"));Assert.That(copy.Owns("paint_3"),Is.True);Assert.That(copy.credits,Is.EqualTo(4321));Assert.That(copy.completedMask,Is.EqualTo(7));
            copy.Build(1).Set(Catalog.Stock(GarageSlot.Paint),Catalog);var next=RecoveryProfile.Parse(JsonUtility.ToJson(copy));Assert.That(next.Build(1).Item(GarageSlot.Paint,Catalog),Is.EqualTo("stock_Paint"));
        }
        [Test] public void MigrationDoesNotReplaceAnExplicitFittedFinish()
        {
            var p=new RecoveryProfile{starterChosen=true,starterVehicle=0,ownedVehicles=1,paint=3};p.ownedParts.Add("paint_2");p.Build(0).Items=new[]{"paint_2"};
            var copy=RecoveryProfile.Parse(JsonUtility.ToJson(p));Assert.That(copy.Build(0).Item(GarageSlot.Paint,Catalog),Is.EqualTo("paint_2"));Assert.That(copy.Owns("paint_3"),Is.False);
        }
        [Test] public void ExplicitStockBeatsLegacyFallbackAndMissingPaintUsesFallback()
        {var b=new RecoveryBuild();Assert.That(RecoveryPaintPalette.Resolve(b,Catalog,3),Is.EqualTo(3));b.Set(Catalog.Stock(GarageSlot.Paint),Catalog);Assert.That(RecoveryPaintPalette.Resolve(b,Catalog,3),Is.Zero);}
        [Test] public void PreviewPurchaseRefitAndSavedSetupsKeepSeparateFinishes()
        {
            var p=new RecoveryProfile();RecoveryEconomy.ChooseStarter(p,0,2);p.credits=100;
            var preview=RecoveryGarageWorkflow.Preview(p.Build(0),Catalog.Find("paint_3"),Catalog);
            Assert.That(RecoveryPaintPalette.Resolve(preview,Catalog),Is.EqualTo(3));Assert.That(RecoveryPaintPalette.Resolve(p.Build(0),Catalog),Is.EqualTo(2));Assert.That(p.credits,Is.EqualTo(100));
            p.presets.Add(new GaragePreset{Name="old",Build=p.Build(0).Copy()});Assert.That(RecoveryGarageWorkflow.TryPrepareFit(p,Catalog.Find("paint_3"),Catalog,out var fitted),Is.True);
            p.builds[0]=fitted;Assert.That(p.credits,Is.Zero);Assert.That(RecoveryPaintPalette.Resolve(p.presets[0].Build,Catalog),Is.EqualTo(2));
            Assert.That(RecoveryGarageWorkflow.TryPrepareFit(p,Catalog.Find("paint_2"),Catalog,out var freeRefit),Is.True);Assert.That(p.credits,Is.Zero);Assert.That(RecoveryPaintPalette.Resolve(freeRefit,Catalog),Is.EqualTo(2));
        }
        [Test] public void AtlasCoversAllBodyMaterialsAndPreservesUnknownTextures()
        {
            var palette=RecoveryPaintPalette.Current;Assert.That(palette,Is.Not.Null);Assert.That(palette.Atlases,Has.Length.EqualTo(2));
            foreach(var atlas in palette.Atlases){Assert.That(atlas.Source,Is.Not.Null);Assert.That(atlas.Colors,Has.Length.EqualTo(4));Assert.That(atlas.Colors.All(t=>t!=null),Is.True);Assert.That(palette.TextureFor(atlas.Source,0,0,false),Is.SameAs(atlas.Source));}
            foreach(var part in Catalog.Parts.Where(p=>p.Slot==GarageSlot.Assembly&&p.Price>0))foreach(var material in part.Prefabs[0].GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&m.name.Contains("Main_Material")))
                Assert.That(palette.Atlases.Any(a=>a.Source==material.GetTexture("_BaseMap")),Is.True,part.EnglishName+" paint source");
            var other=new Texture2D(2,2);try{Assert.That(palette.TextureFor(other,2,0,true),Is.SameAs(other));}finally{UnityEngine.Object.DestroyImmediate(other);}
        }
        [Test] public void PaintMaskExcludesTrimRowsAndOtherPaletteColumns()
        {Assert.That(RecoveryPaintPalette.InPaintStrip(0,128,512,512),Is.True);Assert.That(RecoveryPaintPalette.InPaintStrip(127,511,512,512),Is.True);Assert.That(RecoveryPaintPalette.InPaintStrip(128,511,512,512),Is.False);Assert.That(RecoveryPaintPalette.InPaintStrip(0,127,512,512),Is.False);}
        [Test] public void EveryFinishLeavesVehiclePerformanceUnchanged()
        {
            var builds=Enumerable.Range(0,4).Select(model=>new RecoveryBuild{Model=model}).ToList();
            foreach(var body in Catalog.Parts.Where(p=>p.Slot==GarageSlot.Assembly&&p.Price>0)){var build=new RecoveryBuild();build.Set(body,Catalog);builds.Add(build);}
            foreach(var build in builds){var expected=GaragePerformance.Calculate(build,Catalog);for(int color=0;color<4;color++){var painted=build.Copy();painted.Set(Catalog.Find(color==0?"stock_Paint":"paint_"+color),Catalog);Assert.That(GaragePerformance.Calculate(painted,Catalog),Is.EqualTo(expected));}}
        }
    }
}

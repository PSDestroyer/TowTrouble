using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowTrouble.Tests
{
    public sealed class RecoveryTrackSurfaceTests
    {
        [Test] public void PaintProducesDistinctTracksWithoutChangingGrip()
        {
            Assert.That(RecoveryTrackSurface.FromPaint(1,0,0,0,0),Is.EqualTo(RecoveryTrackKind.Grass));
            Assert.That(RecoveryTrackSurface.FromPaint(.2f,.8f,0,0,0),Is.EqualTo(RecoveryTrackKind.Dirt));
            Assert.That(RecoveryTrackSurface.FromPaint(0,0,1,0,0),Is.EqualTo(RecoveryTrackKind.Sand));
            Assert.That(RecoveryTrackSurface.FromPaint(0,0,0,1,0),Is.EqualTo(RecoveryTrackKind.Hard));
            Assert.That(RecoveryTrackSurface.FromPaint(0,0,0,0,1),Is.EqualTo(RecoveryTrackKind.Snow));
            var map=ScriptableObject.CreateInstance<RecoveryGroundMap>();
            try
            {
                map.Configure(Vector2.zero,Vector2.one*10,2,new byte[]{0,0,1,2});
                map.ConfigureTrackKinds(new byte[]{3,0,1,2});
                Assert.That(map.TryTrackKind(Vector3.zero,out var kind),Is.True);
                Assert.That(kind,Is.EqualTo(RecoveryTrackKind.Grass));
                Assert.That(map.TryTrackKind(new Vector3(10,0,0),out kind),Is.True);
                Assert.That(kind,Is.EqualTo(RecoveryTrackKind.Dirt));
                Assert.That(map.TryTrackKind(new Vector3(11,0,0),out _),Is.False);
                Assert.That(map.Sample(Vector3.zero),Is.EqualTo(RecoveryGroundKind.Rock));
                Assert.That(map.Sample(new Vector3(0,0,10)),Is.EqualTo(RecoveryGroundKind.Sand));
                Assert.Throws<System.ArgumentException>(()=>map.ConfigureTrackKinds(new byte[]{9,0,0,0}));
            }
            finally {Object.DestroyImmediate(map);}
        }
        [Test] public void VisualBoundaryBlendsContinuouslyAndPreservesTraction()
        {
            var map=ScriptableObject.CreateInstance<RecoveryGroundMap>();
            try
            {
                map.Configure(Vector2.zero,Vector2.one*2,2,new byte[]{0,1,0,1});
                map.ConfigureTrackKinds(new byte[]{0,3,0,3});
                float previousGrass=-1;
                for(int step=0;step<=40;step++)
                {
                    var point=new Vector3(step*.05f,0,1);
                    Assert.That(map.TryTrackBlend(point,RecoveryTrackKind.Grass,out float grass,out int mask),Is.True);
                    Assert.That(map.TryTrackBlend(point,RecoveryTrackKind.Dirt,out float dirt,out _),Is.True);
                    Assert.That(grass+dirt,Is.EqualTo(1).Within(.0001f));
                    Assert.That(grass,Is.GreaterThanOrEqualTo(previousGrass));
                    if(step>0)Assert.That(grass-previousGrass,Is.LessThan(.04f));
                    if(step<40)Assert.That(mask,Is.EqualTo((1<<0)|(1<<3)));previousGrass=grass;
                }
                map.TryTrackBlend(Vector3.one,RecoveryTrackKind.Grass,out float middle,out _);
                Assert.That(middle,Is.EqualTo(.5f).Within(.0001f));
                Assert.That(map.Sample(new Vector3(.5f,0,1)),Is.EqualTo(RecoveryGroundKind.Rock));
                Assert.That(map.Sample(new Vector3(1.5f,0,1)),Is.EqualTo(RecoveryGroundKind.Sand));
                Assert.That(map.TryTrackBlend(new Vector3(3,0,1),RecoveryTrackKind.Grass,out _,out _),Is.False);
            }
            finally{Object.DestroyImmediate(map);}
        }
        [Test] public void HardAndWoodHaveNoRutRelief()
        {
            Assert.That(RecoveryTrackSurface.Relief(RecoveryTrackKind.Hard),Is.Zero);
            Assert.That(RecoveryTrackSurface.Relief(RecoveryTrackKind.Wood),Is.Zero);
            Assert.That(RecoveryTrackSurface.Relief(RecoveryTrackKind.Grass),Is.LessThan(RecoveryTrackSurface.Relief(RecoveryTrackKind.Dirt)));
            Assert.That(RecoveryTrackSurface.Relief(RecoveryTrackKind.Dirt),Is.LessThan(RecoveryTrackSurface.Relief(RecoveryTrackKind.Sand)));
            Assert.That(RecoveryTrackSurface.Relief(RecoveryTrackKind.Sand),Is.LessThan(RecoveryTrackSurface.Relief(RecoveryTrackKind.Snow)));
        }
        [Test] public void TracksStayOnTiltedDeckAndStopAtItsEdge()
        {
            var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject("Raised deck",typeof(BoxCollider));SceneManager.MoveGameObjectToScene(go,scene);
            try
            {
                go.transform.position=new Vector3(0,20,0);go.transform.rotation=Quaternion.Euler(10,25,0);
                var deck=go.GetComponent<BoxCollider>();deck.size=new Vector3(4,.2f,8);
                Physics.SyncTransforms();var top=go.transform.TransformPoint(Vector3.up*.1f);
                Assert.That(RecoveryWheelTracks.ProjectContact(deck,top,go.transform.up,out var p,out var n),Is.True);
                Assert.That(Vector3.Distance(p,top),Is.LessThan(.001f));
                Assert.That(Vector3.Dot(n,go.transform.up),Is.GreaterThan(.999f));
                Assert.That(p.y,Is.GreaterThan(19));
                Assert.That(RecoveryWheelTracks.ProjectContact(deck,top+go.transform.right*3,go.transform.up,out _,out _),Is.False);
            }
            finally {Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void WoodUsesOverrideAndLivingMudIsNotDoubleStamped()
        {
            var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject("Contact fixture",typeof(BoxCollider));SceneManager.MoveGameObjectToScene(go,scene);
            var owner=new GameObject("Track fixture",typeof(RecoveryWheelTracks));SceneManager.MoveGameObjectToScene(owner,scene);
            try
            {
                owner.SetActive(false);go.transform.position=new Vector3(0,40,0);Physics.SyncTransforms();var collider=go.GetComponent<BoxCollider>();
                Assert.That(collider.Raycast(new Ray(go.transform.position+Vector3.up*2,Vector3.down),out var hit,4),Is.True);
                var tracks=owner.GetComponent<RecoveryWheelTracks>();
                Assert.That(tracks.TrySurface(hit,out int kind),Is.True);Assert.That(kind,Is.EqualTo((int)RecoveryTrackKind.Hard));
                go.AddComponent<RecoveryTrackSurface>().Configure(RecoveryTrackKind.Wood);
                Assert.That(tracks.TrySurface(hit,out kind),Is.True);Assert.That(kind,Is.EqualTo((int)RecoveryTrackKind.Wood));
                go.AddComponent<RecoveryMudTerrain>();Assert.That(tracks.TrySurface(hit,out _),Is.False);
            }
            finally {Object.DestroyImmediate(owner);Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void TerrainHolesAndOutsideBoundsRejectProjection()
        {
            var scene=EditorSceneManager.NewPreviewScene();var data=new TerrainData {heightmapResolution=33,size=new Vector3(20,5,20)};
            var go=Terrain.CreateTerrainGameObject(data);SceneManager.MoveGameObjectToScene(go,scene);
            try
            {
                var collider=go.GetComponent<TerrainCollider>();
                Assert.That(RecoveryWheelTracks.ProjectContact(collider,new Vector3(10,0,10),Vector3.up,out _,out _),Is.True);
                Assert.That(RecoveryWheelTracks.ProjectContact(collider,new Vector3(21,0,10),Vector3.up,out _,out _),Is.False);
                data.SetHoles(16,16,new bool[,]{{false}});
                Assert.That(RecoveryWheelTracks.ProjectContact(collider,new Vector3(10.1f,0,10.1f),Vector3.up,out _,out _),Is.False);
            }
            finally {Object.DestroyImmediate(go);Object.DestroyImmediate(data);EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}

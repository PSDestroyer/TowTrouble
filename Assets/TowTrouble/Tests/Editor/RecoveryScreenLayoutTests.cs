using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace TowTrouble.Tests
{
    public sealed class RecoveryScreenLayoutTests
    {
        [TestCase(1280,720)][TestCase(1280,800)][TestCase(1024,768)]
        [TestCase(3440,1440)][TestCase(3840,1080)][TestCase(1080,1920)]
        public void ContentFitsSafeAreaWithoutChangingItsAspect(int width,int height)
        {
            var safe=new Rect(.05f,.04f,.9f,.92f);var content=RecoveryScreenLayout.Content(width/(float)height,safe);
            Assert.That(content.xMin,Is.GreaterThanOrEqualTo(safe.xMin-.0001f));Assert.That(content.xMax,Is.LessThanOrEqualTo(safe.xMax+.0001f));
            Assert.That(content.yMin,Is.GreaterThanOrEqualTo(safe.yMin-.0001f));Assert.That(content.yMax,Is.LessThanOrEqualTo(safe.yMax+.0001f));
            Assert.That(content.width*width/(content.height*height),Is.EqualTo(16f/9).Within(.0001f));
            Assert.That(Vector2.Distance(content.center,safe.center),Is.LessThan(.0001f));
        }
        [TestCase(1280,720)][TestCase(1280,800)][TestCase(1024,768)]
        [TestCase(2560,1080)][TestCase(3440,1440)][TestCase(3840,1080)][TestCase(1080,1920)]
        public void FocusedGeometryFitsBetweenTheGaragePanels(int width,int height)
        {
            var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject("Screen framing test");SceneManager.MoveGameObjectToScene(go,scene);
            try
            {
                var camera=go.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=57;camera.aspect=width/(float)height;
                foreach(var safe in new[]{new Rect(0,0,1,1),new Rect(.05f,.05f,.9f,.9f)})
                foreach(var size in new[]{new Vector3(2.38f,3.02f,5.04f),new Vector3(3.3f,3.6f,7.8f)})
                {
                    var body=new Bounds(new Vector3(0,1.15f,-.18f),size);
                    foreach(var slot in new[]{GarageSlot.Assembly,GarageSlot.FrontBumper,GarageSlot.RearBumper,GarageSlot.Wheels})
                    {
                        var detail=RecoveryGarageView.Fallback(slot,body);var viewport=RecoveryGarageView.Viewport(camera.aspect,safe);
                        var frame=RecoveryGarageView.Frame(slot,body,detail,57,camera.aspect,viewport);
                        RecoveryGarageView.Pose(frame.Focus,RecoveryGarageView.Direction(frame.Yaw,frame.Elevation),frame.Distance,57,camera.aspect,viewport,out var position,out var look);
                        camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(look-position));
                        var focus=camera.WorldToViewportPoint(frame.Focus);Assert.That(Vector2.Distance(focus,viewport.center),Is.LessThan(.0001f));
                        for(int i=0;i<8;i++)
                        {
                            var corner=detail.center+Vector3.Scale(detail.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                            var point=camera.WorldToViewportPoint(corner);
                            Assert.That(point.z,Is.GreaterThan(0));Assert.That(point.x,Is.InRange(viewport.xMin-.0001f,viewport.xMax+.0001f),slot+" horizontal framing");
                            Assert.That(point.y,Is.InRange(viewport.yMin-.0001f,viewport.yMax+.0001f),slot+" vertical framing");
                        }
                    }
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void DisplayChoicesKeepReadableModesAndRetainSmallHardwareFallback()
        {
            var modes=new[]{new Resolution{width=800,height=600},new Resolution{width=1280,height=720},new Resolution{width=1920,height=1080}};
            Assert.That(RecoveryMenuSettings.Resolutions(modes,1024,576),Is.EqualTo(new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}));
            Assert.That(RecoveryMenuSettings.Resolutions(new[]{modes[0]},800,600),Is.EqualTo(new[]{new Vector2Int(800,600)}));
        }
    }
}

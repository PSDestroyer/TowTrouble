using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryPlatformTests
    {
        [Test] public void SinglePlayerTargetRedirectsOnlyMultiplayerScreens()
        {
            bool previous=RecoveryPlatform.PreviewSinglePlayerOnly;
            try
            {
                RecoveryPlatform.PreviewSinglePlayerOnly=true;
                Assert.That(RecoveryPlatform.MultiplayerAvailable,Is.False);
                foreach(var page in new[]{"Coop","Lobby","TextEntry"})Assert.That(RecoveryPlatform.MenuPage(page),Is.EqualTo("Main"));
                foreach(var page in new[]{"Title","Main","Starter","Busy","Gameplay","Garage","Board","Settings","Help"})Assert.That(RecoveryPlatform.MenuPage(page),Is.EqualTo(page));
            }
            finally{RecoveryPlatform.PreviewSinglePlayerOnly=previous;}
        }
        [Test] public void MultiplayerEntryPointsReturnBeforeProfileOrNetworkAccessOnSinglePlayerTarget()
        {
            bool previous=RecoveryPlatform.PreviewSinglePlayerOnly;var scene=EditorSceneManager.NewPreviewScene();GameObject go=null;
            try
            {
                RecoveryPlatform.PreviewSinglePlayerOnly=true;go=new GameObject("Inactive session guard test");go.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
                var session=go.AddComponent<DemoSession>();
                Assert.DoesNotThrow(()=>session.Host(false,true));Assert.DoesNotThrow(()=>session.Join("192.168.1.20"));Assert.DoesNotThrow(()=>session.Join(null));
                Assert.That(session.Connecting,Is.False);Assert.That(session.Connected,Is.False);
            }
            finally{if(go!=null)Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);RecoveryPlatform.PreviewSinglePlayerOnly=previous;}
        }
        [Test] public void DesktopKeepsItsMultiplayerScreens()
        {
#if !UNITY_SWITCH
            bool previous=RecoveryPlatform.PreviewSinglePlayerOnly;
            try{RecoveryPlatform.PreviewSinglePlayerOnly=false;Assert.That(RecoveryPlatform.MultiplayerAvailable,Is.True);Assert.That(RecoveryPlatform.MenuPage("Coop"),Is.EqualTo("Coop"));Assert.That(RecoveryPlatform.MenuPage("Lobby"),Is.EqualTo("Lobby"));}
            finally{RecoveryPlatform.PreviewSinglePlayerOnly=previous;}
#endif
        }
    }
}

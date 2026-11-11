using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowTrouble.Tests
{
    public sealed class RecoveryContractIntroTests
    {
        private static void WithIncident(Action<RecoveryVehicle,RecoveryContract,Transform> action)
        {
            var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Inactive incident fixture");root.SetActive(false);SceneManager.MoveGameObjectToScene(root,scene);
            try
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TowTrouble/Prefabs/Scout.prefab"),scene);
                go.transform.SetParent(root.transform,false);var car=go.GetComponent<RecoveryVehicle>();car.SetModel(0);
                var contract=new RecoveryContract(1,0,"Test","","","",new Vector3(10000,10000,10000),Vector3.zero,10,0)
                {IsOpenWorld=true,Heading=37,Pitch=18,Roll=-22,SituationKind=RecoverySituationKind.Ditch};
                car.transform.SetPositionAndRotation(contract.Target,Quaternion.Euler(contract.Pitch,contract.Heading,contract.Roll));
                action(car,contract,root.transform);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void CinematicUsesEachVehiclesApprovedLoopsAndRespectsMuteAndReturn()
        {
            var bank=Resources.Load<RecoveryAudioBank>("RecoveryAudioBank");Assert.That(bank,Is.Not.Null);
            WithIncident((car,contract,parent)=>
            {
                for(int model=0;model<4;model++)
                {
                    car.SetModel(model);
                    using(var intro=new RecoveryContractIntro(car,contract,parent,false))
                    {
                        intro.ConfigureAudio(bank,.8f);intro.ConfigureAudio(bank,.8f);
                        var actor=parent.Find("Recovery incident reenactment");var sources=actor.GetComponents<AudioSource>();
                        Assert.That(sources.Length,Is.EqualTo(2));
                        Assert.That(sources[0].clip,Is.SameAs(model==0?bank.ScoutMuscleIdle:bank.EngineFor(model).Idle));
                        Assert.That(sources[1].clip,Is.SameAs(model==0?bank.ScoutMuscleLow:bank.EngineFor(model).Low));
                        foreach(var source in sources){Assert.That(source.outputAudioMixerGroup,Is.SameAs(bank.Vehicles));Assert.That(source.playOnAwake,Is.False);}
                        intro.Advance(1);float lastPitch=sources[1].pitch;
                        for(int step=0;step<40;step++)
                        {
                            intro.Advance(.1f);
                            Assert.That(sources[0].pitch,Is.EqualTo(1));
                            Assert.That(Mathf.Abs(sources[1].pitch-lastPitch),Is.LessThan(.025f));lastPitch=sources[1].pitch;
                            Assert.That(sources[0].volume+sources[1].volume,Is.LessThan(.17f));
                        }
                        intro.Advance(0,false);foreach(var source in sources)Assert.That(source.volume,Is.Zero);
                        intro.Advance(1);Assert.That(actor.gameObject.activeSelf,Is.False);
                    }
                }
                using(var muted=new RecoveryContractIntro(car,contract,parent,false))
                {
                    muted.ConfigureAudio(bank,0);muted.Advance(1);
                    foreach(var source in parent.Find("Recovery incident reenactment").GetComponents<AudioSource>())Assert.That(source.volume,Is.Zero);
                }
                contract.SituationKind=RecoverySituationKind.EngineFailure;
                using(var failed=new RecoveryContractIntro(car,contract,parent,false))
                {
                    failed.ConfigureAudio(bank,1);failed.Advance(3);
                    foreach(var source in parent.Find("Recovery incident reenactment").GetComponents<AudioSource>())Assert.That(source.volume,Is.Zero);
                }
            });
        }
        [Test] public void ReenactmentMovesOnlyVisualCopyAndReturnsEveryOriginalVisibilityFlag()
        {
            WithIncident((source,contract,parent)=>
            {
                var renderers=source.GetComponentsInChildren<Renderer>(true);renderers[0].forceRenderingOff=true;
                var originalPosition=source.transform.position;var originalRotation=source.transform.rotation;var originalModel=source.ModelIndex;
                using(var intro=new RecoveryContractIntro(source,contract,parent,false))
                {
                    var actor=parent.Find("Recovery incident reenactment");
                    Assert.That(actor.GetComponentsInChildren<Collider>(true),Is.Empty);
                    Assert.That(actor.GetComponentsInChildren<Rigidbody>(true),Is.Empty);
                    Assert.That(actor.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);
                    var start=intro.ActorPosition;intro.Advance(3.5f);
                    Assert.That(Vector3.Distance(start,intro.ActorPosition),Is.GreaterThan(3));
                    Assert.That(Vector3.Distance(intro.ActorPosition,contract.Target),Is.LessThan(.001f));
                    Assert.That(source.transform.position,Is.EqualTo(originalPosition));Assert.That(Quaternion.Angle(source.transform.rotation,originalRotation),Is.LessThan(.001f));Assert.That(source.ModelIndex,Is.EqualTo(originalModel));
                    foreach(var renderer in renderers)Assert.That(renderer.forceRenderingOff,Is.True);
                    intro.Advance(2);Assert.That(actor.gameObject.activeSelf,Is.False);
                    Assert.That(renderers[0].forceRenderingOff,Is.True);for(int i=1;i<renderers.Length;i++)Assert.That(renderers[i].forceRenderingOff,Is.False);
                }
                Assert.That(parent.Find("Recovery incident reenactment"),Is.Null);
            });
        }
        [Test] public void SkipUsesOpaqueReturnBeforeControlIsReleased()
        {
            WithIncident((car,contract,parent)=>
            {
                using(var intro=new RecoveryContractIntro(car,contract,parent,true))
                {
                    intro.Skip();Assert.That(intro.Elapsed,Is.Zero);
                    intro.Advance(1);intro.Skip();Assert.That(intro.Returning,Is.False);Assert.That(intro.Finished,Is.False);
                    intro.Advance(.3f);Assert.That(intro.Fade,Is.EqualTo(1).Within(.001f));Assert.That(intro.Returning,Is.True);Assert.That(intro.Finished,Is.False);
                    intro.Advance(.35f);Assert.That(intro.Finished,Is.True);Assert.That(intro.Fade,Is.EqualTo(0).Within(.001f));
                }
            });
        }
        [Test] public void IncidentIsRevealedOnlyAfterTheOutgoingViewHasFadedToBlack()
        {
            WithIncident((car,contract,parent)=>
            {
                var renderer=car.CurrentModel.Root.GetComponentInChildren<Renderer>(true);
                using(var intro=new RecoveryContractIntro(car,contract,parent,false))
                {
                    intro.Advance(.24f);Assert.That(intro.ShowingIncident,Is.False);Assert.That(renderer.forceRenderingOff,Is.False);
                    Assert.That(parent.Find("Recovery incident reenactment").gameObject.activeSelf,Is.False);
                    intro.Advance(.01f);Assert.That(intro.ShowingIncident,Is.True);Assert.That(intro.Fade,Is.EqualTo(1).Within(.001f));
                    Assert.That(renderer.forceRenderingOff,Is.True);
                }
            });
        }
        [Test] public void EarlyCancellationRestoresRendererStateAndRemovesActor()
        {
            WithIncident((car,contract,parent)=>
            {
                var renderer=car.CurrentModel.Root.GetComponentInChildren<Renderer>(true);
                var intro=new RecoveryContractIntro(car,contract,parent,false);intro.Advance(1);intro.Dispose();intro.Dispose();
                Assert.That(renderer.forceRenderingOff,Is.False);Assert.That(parent.Find("Recovery incident reenactment"),Is.Null);
            });
        }
        [Test] public void ReducedMotionEndsAtAuthoredPitchRollWithoutRocking()
        {
            WithIncident((car,contract,parent)=>
            {
                using(var intro=new RecoveryContractIntro(car,contract,parent,true))
                {
                    intro.Advance(3.6f);var actor=parent.Find("Recovery incident reenactment");
                    Assert.That(Quaternion.Angle(actor.rotation,Quaternion.Euler(contract.Pitch,contract.Heading,contract.Roll)),Is.LessThan(.001f));
                    var expected=actor.rotation;intro.Advance(.15f);Assert.That(Quaternion.Angle(actor.rotation,expected),Is.LessThan(.001f));
                }
            });
        }
    }
}

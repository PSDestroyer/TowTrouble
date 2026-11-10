using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TowTrouble.Tests
{
    public sealed class RecoveryButtonSelectionTests
    {
        private static readonly Color Paper = new Color(1,.96f,.83f), Ink = new Color(.065f,.075f,.095f,.94f);
        private static RecoveryPlateGraphic Plate(UnityEngine.UI.Button button) => button.GetComponentInChildren<RecoveryPlateGraphic>();
        private static void WithButtons(Action<EventSystem,UnityEngine.UI.Button,UnityEngine.UI.Button> check)
        {
            var previous = EventSystem.current; var selection = previous?.currentSelectedGameObject;
            var scene = EditorSceneManager.NewPreviewScene(); var root = new GameObject("Disposable button selection", typeof(RectTransform), typeof(Canvas)); SceneManager.MoveGameObjectToScene(root,scene);
            TMP_FontAsset font = null;
            try
            {
                font = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TowTrouble/Art/UI/Theme/Fonts/TowText SDF.asset"));
                var events = new GameObject("Preview EventSystem",typeof(EventSystem)); events.transform.SetParent(root.transform,false); var es = events.GetComponent<EventSystem>();
                if(EventSystem.current!=es)typeof(EventSystem).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(es,null);
                EventSystem.current = es;
                UnityEngine.UI.Button Make(string name,bool preferred)
                {
                    var go = new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Button)); go.transform.SetParent(root.transform,false);
                    var art = new GameObject("Plate",typeof(RectTransform),typeof(RecoveryPlateGraphic)); art.transform.SetParent(go.transform,false);
                    var label = new GameObject("Caption",typeof(RectTransform),typeof(TextMeshProUGUI)); label.transform.SetParent(go.transform,false); var text = label.GetComponent<TextMeshProUGUI>(); text.font = font; text.color = Paper; text.text = name;
                    var button = go.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic = art.GetComponent<RecoveryPlateGraphic>(); go.AddComponent<RecoveryUiFeedback>().ConfigureButton(preferred); return button;
                }
                var confirm = Make("Confirm",true); var cancel = Make("Cancel",false);
                var nav = confirm.navigation; nav.mode = UnityEngine.UI.Navigation.Mode.Explicit; nav.selectOnRight = cancel; confirm.navigation = nav;
                nav = cancel.navigation; nav.mode = UnityEngine.UI.Navigation.Mode.Explicit; nav.selectOnLeft = confirm; cancel.navigation = nav;
                check(es,confirm,cancel); es.SetSelectedGameObject(null);
            }
            finally
            {
                var events = root.GetComponentInChildren<EventSystem>(); if(events!=null)typeof(EventSystem).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(events,null);
                UnityEngine.Object.DestroyImmediate(root);
                // The font clone borrows the source asset's atlas/material; TMP owns their destruction.
                if(font!=null){font.atlasTextures=Array.Empty<Texture2D>();font.material=null;UnityEngine.Object.DestroyImmediate(font);}
                EditorSceneManager.ClosePreviewScene(scene);
                if(previous!=null){EventSystem.current=previous;previous.SetSelectedGameObject(selection);}
            }
        }
        [Test] public void RecommendedActionIsGreyWhenCancelHasTheRealSelection()
        {
            WithButtons((es,confirm,cancel)=>
            {
                Assert.That(Plate(confirm).Style,Is.EqualTo(RecoveryPlateStyle.Charcoal));
                es.SetSelectedGameObject(cancel.gameObject);
                Assert.That(Plate(cancel).Style,Is.EqualTo(RecoveryPlateStyle.Gold)); Assert.That(Plate(confirm).Style,Is.EqualTo(RecoveryPlateStyle.Charcoal));
                Assert.That(confirm.GetComponent<RecoveryUiFeedback>().PreferredFocus,Is.True);
                Assert.That(cancel.GetComponentInChildren<TMP_Text>().color,Is.EqualTo(Ink)); Assert.That(confirm.GetComponentInChildren<TMP_Text>().color,Is.EqualTo(Paper));
                es.SetSelectedGameObject(confirm.gameObject);
                Assert.That(Plate(confirm).Style,Is.EqualTo(RecoveryPlateStyle.Gold)); Assert.That(Plate(cancel).Style,Is.EqualTo(RecoveryPlateStyle.Charcoal));
                Assert.That(cancel.GetComponentInChildren<TMP_Text>().color,Is.EqualTo(Paper));
            });
        }
        [Test] public void DirectionalNavigationTransfersTheGoldFillInBothDirections()
        {
            WithButtons((es,confirm,cancel)=>
            {
                es.SetSelectedGameObject(confirm.gameObject);
                ExecuteEvents.Execute(confirm.gameObject,new AxisEventData(es){moveDir=MoveDirection.Right},ExecuteEvents.moveHandler);
                Assert.That(es.currentSelectedGameObject,Is.SameAs(cancel.gameObject)); Assert.That(Plate(cancel).Style,Is.EqualTo(RecoveryPlateStyle.Gold)); Assert.That(Plate(confirm).Style,Is.EqualTo(RecoveryPlateStyle.Charcoal));
                ExecuteEvents.Execute(cancel.gameObject,new AxisEventData(es){moveDir=MoveDirection.Left},ExecuteEvents.moveHandler);
                Assert.That(es.currentSelectedGameObject,Is.SameAs(confirm.gameObject)); Assert.That(Plate(confirm).Style,Is.EqualTo(RecoveryPlateStyle.Gold)); Assert.That(Plate(cancel).Style,Is.EqualTo(RecoveryPlateStyle.Charcoal));
            });
        }
        [Test] public void PointerFocusAndKeyboardFocusCannotLeaveTwoGoldButtons()
        {
            WithButtons((es,confirm,cancel)=>
            {
                ExecuteEvents.Execute(confirm.gameObject,new PointerEventData(es),ExecuteEvents.pointerEnterHandler);
                Assert.That(es.currentSelectedGameObject,Is.SameAs(confirm.gameObject));
                es.SetSelectedGameObject(cancel.gameObject); // Mouse may still be parked over Confirm.
                Assert.That(Plate(cancel).Style,Is.EqualTo(RecoveryPlateStyle.Gold)); Assert.That(Plate(confirm).Style,Is.EqualTo(RecoveryPlateStyle.Charcoal));
                ExecuteEvents.Execute(confirm.gameObject,new PointerEventData(es),ExecuteEvents.pointerEnterHandler);
                Assert.That(es.currentSelectedGameObject,Is.SameAs(confirm.gameObject)); Assert.That(Plate(confirm).Style,Is.EqualTo(RecoveryPlateStyle.Gold)); Assert.That(Plate(cancel).Style,Is.EqualTo(RecoveryPlateStyle.Charcoal));
            });
        }
        [Test] public void DisabledButtonLosesGoldAndCannotStealPointerFocus()
        {
            WithButtons((es,confirm,cancel)=>
            {
                es.SetSelectedGameObject(confirm.gameObject); confirm.interactable=false;
                typeof(RecoveryUiFeedback).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(confirm.GetComponent<RecoveryUiFeedback>(),null);
                Assert.That(Plate(confirm).Style,Is.EqualTo(RecoveryPlateStyle.Charcoal));
                es.SetSelectedGameObject(cancel.gameObject); ExecuteEvents.Execute(confirm.gameObject,new PointerEventData(es),ExecuteEvents.pointerEnterHandler);
                Assert.That(es.currentSelectedGameObject,Is.SameAs(cancel.gameObject)); Assert.That(Plate(cancel).Style,Is.EqualTo(RecoveryPlateStyle.Gold));
            });
        }
        [Test] public void HintContrastFollowsSelectionWhileInsetBadgesKeepTheirOwnColors()
        {
            WithButtons((es,confirm,cancel)=>
            {
                var cap = new GameObject("Hint",typeof(RectTransform),typeof(UnityEngine.UI.Image)); cap.transform.SetParent(confirm.transform,false);
                var key = new GameObject("Key",typeof(RectTransform),typeof(TextMeshProUGUI)); key.transform.SetParent(cap.transform,false);
                var inset = new GameObject("Inset",typeof(RectTransform)); inset.transform.SetParent(confirm.transform,false);
                var badge = new GameObject("Badge",typeof(RectTransform),typeof(RecoveryIconGraphic)); badge.transform.SetParent(inset.transform,false); var icon=badge.GetComponent<RecoveryIconGraphic>(); icon.color=Color.green;
                var image=cap.GetComponent<UnityEngine.UI.Image>(); var label=key.GetComponent<TMP_Text>(); label.font=confirm.GetComponentInChildren<TMP_Text>().font; confirm.GetComponent<RecoveryUiFeedback>().ConfigureHint(image,label);
                es.SetSelectedGameObject(confirm.gameObject); Assert.That(image.color,Is.EqualTo(Ink)); Assert.That(label.color,Is.EqualTo(Paper)); Assert.That(icon.color,Is.EqualTo(Color.green));
                es.SetSelectedGameObject(cancel.gameObject); Assert.That(image.color,Is.EqualTo(Paper)); Assert.That(label.color,Is.EqualTo(Ink)); Assert.That(icon.color,Is.EqualTo(Color.green));
            });
        }
    }
}

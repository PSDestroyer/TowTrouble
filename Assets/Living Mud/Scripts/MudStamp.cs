using JetBrains.Annotations;
using System.Collections;
using UnityEngine;

namespace Dinopunch.LivingMud
{
    /// <summary>
    /// Controlling class for a stamp that will draw its texture to a Custom Render Texture that displays
    /// on a Dynamic Mud Surface.  The mud stamp uses a shader and material to interact with the Custom Render Texture.
    /// </summary>
    [SelectionBase]
    public class MudStamp : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Normal texture with alpha that stamp will draw on the Custom Render Texture.")]
        public Texture2D StampTexture;

        [Tooltip("Material that uses the Stamp shader to draw to the Custom Render Texture")]
        public Material StampMaterial;

        [Tooltip("Reference to the mud surface that stamp is on")]
        [SerializeField] DynamicMudSurface dynamicMudSurface;
        public DynamicMudSurface DynamicMudSurface => dynamicMudSurface;

        [Header("Settings")]
        [Tooltip("Instruct the shader drawing the stamp to warp the stamp toward the current foward direction of the stamp.  Useful for long tracks in the mud.")]
        [SerializeField] bool bendWithRotation = false;

        private bool flipX;
        private bool flipY;
        private CustomRenderTexture stampCustomRenderTex;
        private float bendAmount = 0.0f;

        public void SetFlipX(bool flipX) => this.flipX = flipX;
        public void SetFlipY(bool flipY) => this.flipY = flipY;
        public void SetSurface(DynamicMudSurface newSurface) => dynamicMudSurface = newSurface;

        public void SetBendAmount(float rotationAmount)
        {
            bendWithRotation = true;
            rotationAmount = Mathf.Clamp(rotationAmount, -45, 45);
            bendAmount = (rotationAmount * .5f) / 45;
        }

        public void StampSurface()
        {
            //get a reference to the Stamp Custom Render Texture 
            stampCustomRenderTex = dynamicMudSurface.StampRenderTexture;

            //Find out where the stamp is in relation to the texture
            var transformData = GetTransformInTextureSpace(StampTexture);

            var texRelativePosition = new Vector2(transformData.x, transformData.y);
            var scale = transformData.z;
            var localRot = transformData.w;

            //Correct the scale to the size of the surface (Living Mud assumes stamp's native size is 1 unit and surfaces are 10 units)
            scale *= 10.0f / dynamicMudSurface.BoundingBoxSizeInUnits;

            SendStampTexToCustomRenderTex(StampMaterial, StampTexture, texRelativePosition, scale, localRot);

            //Update both buffers of the Stamp CustomRenderTexture on the DMS Material
            stampCustomRenderTex.Update();
            stampCustomRenderTex.Update();
        }

        public Vector4 GetTransformInTextureSpace(Texture2D stampTexture) {

            //Prefab transform world data needs to be converted into texture space based on the stamp texture resolution and the target resolution
            //Send the relevant data to the stampMaterial shader to it can transform the texture and apply to the CustomRenderTexture

            //get the local position of the transform relative to the center of the mud surface
            var surfaceBounds = dynamicMudSurface.GetComponent<Renderer>().bounds;
            var surfaceCenterToOriginOffset = dynamicMudSurface.transform.InverseTransformPoint(surfaceBounds.center);
            var stampLocalPosition = dynamicMudSurface.transform.InverseTransformPoint(transform.position) + surfaceCenterToOriginOffset;

            //offset from the center to the lower left of the surface
            //use the greater of the two axis so that the calculation is a square to match the render texture
            var normalizedExtents = Mathf.Max(surfaceBounds.size.x, surfaceBounds.size.z) * .5f;

            stampLocalPosition += (Vector3.one * normalizedExtents);

            //convert the local position to a (0,0) -> (1,1) coordinate space based on the bounds of the surface
            var worldRelativePosition = new Vector3(stampLocalPosition.x / Mathf.Max(surfaceBounds.size.x, surfaceBounds.size.z),
                                               stampLocalPosition.y / surfaceBounds.size.y,
                                               stampLocalPosition.z / Mathf.Max(surfaceBounds.size.x, surfaceBounds.size.z));

            var texRelativePosition = new Vector2(worldRelativePosition.x, worldRelativePosition.z);

            //get local rotation of the transform relative to the mud surface
            //this relies on the surface and stamp having a Y is up orientation
            var localRot = -1 * (dynamicMudSurface.transform.rotation.eulerAngles.y - transform.rotation.eulerAngles.y);

            //get the scale of the stamp as a proportion of size to the surface render texture
            //this assumes stamp base size is 1 unit and surface is 10 units
            var stampScale = transform.localScale.x;
            var scale = stampScale * .1f;

            Vector4 transformData = new Vector4(texRelativePosition.x, texRelativePosition.y, scale, localRot);
            return transformData;           
        }

        public void SendStampTexToCustomRenderTex(Material stampMat, Texture2D stampTexture, Vector2 texRelativePosition, float scale, float localRot, float rotDifference = 0.0f) {
            //send data to Shader that makes the transformation and applies it to the CustomRenderTexture
            stampMat.SetTexture("_Tex", stampTexture);
            stampMat.SetTexture("_Tex2", stampCustomRenderTex);
            
            stampMat.SetFloat("_PosX", texRelativePosition.x);
            stampMat.SetFloat("_PosY", texRelativePosition.y);
            stampMat.SetFloat("_Scale", scale);
            stampMat.SetFloat("_Degrees", localRot);

            if (flipX)
                stampMat.SetFloat("_FlipX", 1.0f);
            else
                stampMat.SetFloat("_FlipX", 0.0f);

            if (flipY)
                stampMat.SetFloat("_FlipY", 1.0f);
            else
                stampMat.SetFloat("_FlipY", 0.0f);

            //calculate the bend values
            if (bendWithRotation)
                stampMat.SetFloat("_Bend", bendAmount);

            //assign the material with the shader than handles the transform calculations
            stampCustomRenderTex.material = stampMat;          
        }
    }
}
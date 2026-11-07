using UnityEngine;

namespace TowTrouble
{
    // COZY writes RenderSettings in CozyAtmosphereModule.LateUpdate (order 0).
    // Apply the game's visibility floor afterward, without changing the imported module.
    [DefaultExecutionOrder(180)]
    public sealed class RecoveryCozyVisibility : MonoBehaviour
    {
        [SerializeField] private RecoveryCozyEnvironment environment;
        public void Configure(RecoveryCozyEnvironment owner){environment=owner;}
        private void LateUpdate(){if(environment!=null&&environment.isActiveAndEnabled)environment.ApplyVisibility();}
    }
}

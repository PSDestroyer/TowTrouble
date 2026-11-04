/*
http://www.cgsoso.com/forum-211-1.html

CG搜搜 Unity3d 每日Unity3d插件免费更新 更有VIP资源！

CGSOSO 主打游戏开发，影视设计等CG资源素材。

插件由会员免费分享，如果商用，请务必联系原著购买授权！

daily assets update for try.

U should buy a license from author if u use it in your project!
*/

using System.Collections;
using UnityEngine;

/*Sub-component of the main player interaction script, 
  needed for collision detection and playback drop sound*/

namespace Suntail
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(AudioSource))]
    public class PhysicsObject : MonoBehaviour
    {
        [Tooltip("Waiting time for an item to be picked up")]
        [SerializeField] private float waitOnPickup = 0.2f;

        [Tooltip("The force by which an object is pulled away from the parent")]
        [SerializeField] private float breakForce = 25f;

        [Tooltip("Array drop sounds")]
        [SerializeField] private AudioClip[] dropClips;
        [HideInInspector] public bool pickedUp = false;
        [HideInInspector] public bool wasPickedUp = false;
        [HideInInspector] public PlayerInteractions playerInteraction;
        private AudioSource _objectAudioSource;

        private void Awake()
        {
            _objectAudioSource = gameObject.GetComponent<AudioSource>();
        }

        //Breaking connection if break force be lower magnitude
        private void OnCollisionEnter(Collision collision)
        {
            if (pickedUp)
            {
                if (collision.relativeVelocity.magnitude > breakForce)
                {
                    playerInteraction.BreakConnection();
                }

            }
            else if (wasPickedUp) //Check if the item has been picked up
            {
                PlayDropSound(); //Play sound if we drop an object and it hits the ground.
            }

        }

        //Prevent the connection from breaking when you just picked up the object as it sometimes fires a collision with the ground or whatever it is touching
        public IEnumerator PickUp()
        {
            yield return new WaitForSeconds(waitOnPickup);
            pickedUp = true;
            wasPickedUp = true;
        }

        //Playing drop sound on item collision
        private void PlayDropSound()
        {
            _objectAudioSource.clip = dropClips[Random.Range(0, dropClips.Length)];
            _objectAudioSource.Play();
        }
    }
}

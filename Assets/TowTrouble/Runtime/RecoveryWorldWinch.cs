using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryWorld
    {
        public bool ExternalAudio {get;set;}
        private readonly WinchFrame[] winchFrames = new WinchFrame[2];
        private readonly AudioSource[] winchAudio = new AudioSource[2];
        private AudioClip winchMotorClip;
        public WinchFrame Winch(int slot) => slot >= 0 && slot < 2 ? winchFrames[slot] : default;

        private void ResetWinches()
        {
            System.Array.Clear(winchFrames, 0, winchFrames.Length);
            System.Array.Clear(lengths, 0, lengths.Length);
            for (int i=0;i<drivers.Length;i++) drivers[i].SetTowForce(0);
        }

        private void PresentWinches()
        {
            if (!Application.isPlaying||ExternalAudio) return;
            for (int i=0;i<2;i++)
            {
                var frame=winchFrames[i];
                bool active=HasContract&&connected[i]&&Time.timeScale>.01f&&RecoveryVehicle.EffectsGain>0
                    &&(frame.State==WinchState.TakingUp||frame.State==WinchState.Pulling
                        ||frame.State==WinchState.Stalled||frame.State==WinchState.PayingOut);
                if (!active) { if(winchAudio[i]!=null)winchAudio[i].Stop();continue; }
                if(winchAudio[i]==null)CreateWinchAudio(i);
                var audio=winchAudio[i];audio.transform.position=drivers[i].RearHitch;
                float speed=Mathf.Clamp01(Mathf.Abs(frame.Speed)/Mathf.Max(.01f,drivers[i].WinchSpeed));
                float pitch=.55f+speed*.75f;
                audio.pitch=Mathf.MoveTowards(audio.pitch,pitch,Time.unscaledDeltaTime*3);
                audio.volume=(frame.State==WinchState.Stalled?.13f:.07f+.05f*Mathf.Clamp01(frame.Load))*RecoveryVehicle.EffectsGain;
                if(!audio.isPlaying)audio.Play();
            }
        }

        private void CreateWinchAudio(int slot)
        {
            if(winchMotorClip==null)
            {
                // One deterministic, seamless motor loop per world; no gameplay RNG.
                const int sampleRate=22050;
                var samples=new float[sampleRate];
                for(int i=0;i<samples.Length;i++)
                {
                    float t=i/(float)sampleRate;
                    float teeth=.8f+.2f*Mathf.Sin(2*Mathf.PI*28*t);
                    samples[i]=teeth*(Mathf.Sin(2*Mathf.PI*140*t)*.3f+Mathf.Sin(2*Mathf.PI*420*t)*.13f+Mathf.Sin(2*Mathf.PI*840*t)*.06f);
                }
                winchMotorClip=AudioClip.Create("Winch prototype motor",samples.Length,1,sampleRate,false);
                winchMotorClip.SetData(samples,0);
            }
            var motor=new GameObject("Winch motor");motor.transform.SetParent(drivers[slot].transform,false);
            var audio=motor.AddComponent<AudioSource>();audio.playOnAwake=false;audio.loop=true;audio.clip=winchMotorClip;
            var engine=drivers[slot].GetComponent<AudioSource>();if(engine!=null)audio.outputAudioMixerGroup=engine.outputAudioMixerGroup;
            audio.spatialBlend=1;audio.rolloffMode=AudioRolloffMode.Logarithmic;audio.minDistance=5;audio.maxDistance=45;
            audio.pitch=.55f;winchAudio[slot]=audio;
        }

        private void OnDisable()
        { foreach(var audio in winchAudio)if(audio!=null)audio.Stop(); }
        private void OnDestroy()
        {
            foreach(var audio in winchAudio)if(audio!=null)Destroy(audio.gameObject);
            if(winchMotorClip!=null)Destroy(winchMotorClip);
        }
    }
}

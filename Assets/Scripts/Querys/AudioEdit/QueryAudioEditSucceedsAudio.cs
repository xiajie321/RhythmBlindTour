using Qf.Models.AudioEdit;
using QFramework;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Querys.AudioEdit
{
    public class QueryAudioEditSucceedsAudio : AbstractQuery<AudioClip>
    {
        AudioEditModel editModel;
        AudioClip audioClip;
        TheTypeOfOperation theTypeOfOperation;
        public QueryAudioEditSucceedsAudio(TheTypeOfOperation theTypeOfOperation)
        {
            this.theTypeOfOperation = theTypeOfOperation;
        }
        protected override AudioClip OnDo()
        {
            editModel = this.GetModel<AudioEditModel>();
            switch (theTypeOfOperation)
            {
                case TheTypeOfOperation.Click:         // 0
                    audioClip = editModel.ClickSucceedAudioClip;
                    break;
                case TheTypeOfOperation.SwipeDown:     // 1
                    audioClip = editModel.DownSucceedAudioClip;
                    break;
                case TheTypeOfOperation.SwipeUp:       // 2
                    audioClip = editModel.UpSucceedAudioClip;
                    break;
                case TheTypeOfOperation.SwipeLeft:     // 3
                    audioClip = editModel.LeftSucceedAudioClip;
                    break;
                case TheTypeOfOperation.SwipeRight:    // 4
                    audioClip = editModel.RightSucceedAudioClip;
                    break;
                default:
                    audioClip = null;
                    break;
            }
            ;
            return audioClip;
        }
    }
}
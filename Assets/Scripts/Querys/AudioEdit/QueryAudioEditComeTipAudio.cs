using Qf.Models.AudioEdit;
using QFramework;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Querys.AudioEdit
{
    public class QueryAudioEditComeTipAudio : AbstractQuery<AudioClip>
    {
        AudioEditModel editModel;
        AudioClip audioClip;
        TheTypeOfOperation theTypeOfOperation;
        public QueryAudioEditComeTipAudio(TheTypeOfOperation theTypeOfOperation)
        {
            this.theTypeOfOperation = theTypeOfOperation;
        }
        protected override AudioClip OnDo()
        {
            editModel = this.GetModel<AudioEditModel>();
            switch (theTypeOfOperation)
            {
                case TheTypeOfOperation.SwipeUp:
                    audioClip = editModel.UpTipsAudioClip;
                    break;
                case TheTypeOfOperation.SwipeDown:
                    audioClip = editModel.DownTipsAudioClip;
                    break;
                case TheTypeOfOperation.SwipeRight:
                    audioClip = editModel.RightTipsAudioClip;
                    break;
                case TheTypeOfOperation.SwipeLeft:
                    audioClip = editModel.LeftTipsAudioClip;
                    break;
                case TheTypeOfOperation.Click:
                    audioClip = editModel.ClickTipsAudioClip;
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
using Qf.Models.AudioEdit;
using QFramework;
using System.Collections;
using UnityEngine;

namespace Qf.Commands.AudioEdit
{
    public class SetAudioEditAudioComeTipsCommand : AbstractCommand
    {
        AudioEditModel editModel;
        TheTypeOfOperation Type;
        AudioClip audio;
        public SetAudioEditAudioComeTipsCommand(TheTypeOfOperation theTypeOfOperation,AudioClip audioClip)
        {
            Type = theTypeOfOperation;
            audio = audioClip;
        }
        protected override void OnExecute()
        {
            editModel = this.GetModel<AudioEditModel>();
            switch (Type)
            {
                case TheTypeOfOperation.SwipeUp:
                    editModel.UpTipsAudioClip = audio;
                    break;
                case TheTypeOfOperation.SwipeDown:
                    editModel.DownTipsAudioClip = audio;
                    break;
                case TheTypeOfOperation.SwipeRight:
                    editModel.RightTipsAudioClip = audio;
                    break;
                case TheTypeOfOperation.SwipeLeft:
                    editModel.LeftTipsAudioClip = audio;
                    break;
                case TheTypeOfOperation.Click:
                    editModel.ClickTipsAudioClip = audio;
                    break;
                default:
                    break;
            }
        }
    }
}
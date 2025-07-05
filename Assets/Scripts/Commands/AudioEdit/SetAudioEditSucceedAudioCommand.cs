using Qf.Models.AudioEdit;
using QFramework;
using System.Collections;
using UnityEngine;

namespace Qf.Commands.AudioEdit
{
    public class SetAudioEditSucceedAudioCommand : AbstractCommand
    {
        AudioEditModel editModel;
        AudioClip audioClip;
        TheTypeOfOperation TheTypeOfOperation;
        public SetAudioEditSucceedAudioCommand(TheTypeOfOperation theTypeOfOperation,AudioClip value)
        {
            TheTypeOfOperation = theTypeOfOperation;
            audioClip = value;
        }
        protected override void OnExecute()
        {
            editModel = this.GetModel<AudioEditModel>();
            switch (TheTypeOfOperation)
            {
                case TheTypeOfOperation.SwipeUp:
                    editModel.UpSucceedAudioClip = audioClip;
                    break;
                case TheTypeOfOperation.SwipeDown:
                    editModel.DownSucceedAudioClip = audioClip;
                    break;
                case TheTypeOfOperation.SwipeRight:
                    editModel.RightSucceedAudioClip = audioClip;
                    break;
                case TheTypeOfOperation.SwipeLeft:
                    editModel.LeftSucceedAudioClip = audioClip;
                    break;
                case TheTypeOfOperation.Click:
                    editModel.ClickSucceedAudioClip = audioClip;
                    break;
            }
        }
    }
}
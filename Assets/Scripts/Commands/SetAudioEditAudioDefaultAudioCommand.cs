using Qf.Models.AudioEdit;
using QFramework;
using UnityEngine;

namespace Qf.Commands.AudioEdit
{
    public class SetAudioEditAudioDefaultAudioCommand : AbstractCommand
    {
        AudioEditModel editModel;
        AudioClip audioClip;
        public SetAudioEditAudioDefaultAudioCommand(AudioClip value)
        {
            audioClip = value;
        }
        protected override void OnExecute()
        {
            editModel = this.GetModel<AudioEditModel>();
            editModel.DefaultAudioClip = audioClip;
        }
    }
}

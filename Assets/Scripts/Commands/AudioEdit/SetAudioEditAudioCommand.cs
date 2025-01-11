using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;
using UnityEngine;
namespace Qf.Commands.AudioEdit
{
    /// <summary>
    ///���ñ༭����Ƶ����
    /// </summary>
    public class SetAudioEditAudioCommand : AbstractCommand
    {
        AudioClip clip;
        /// <summary>
        /// ���ñ༭����Ƶ����
        /// </summary>
        /// <param name="audioClip">��Ƶ</param>
        public SetAudioEditAudioCommand(AudioClip audioClip)
        {
            clip = audioClip;
        }
        protected override void OnExecute()
        {
            this.GetModel<AudioEditModel>().EditAudioClip = clip;
            this.SendCommand(new SetAudioEditThisTimeCommand(0));
            this.SendEvent<MainAudioChangeValue>();
        }
    }

}

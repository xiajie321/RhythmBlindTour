using Qf.Models.AudioEdit;
using QFramework;
using UnityEngine;
namespace Qf.Commands.AudioEdit
{
    /// <summary>
    ///���ñ༭����Ƶ����
    /// </summary>
    public class SetEditAudioAudioCommand : AbstractCommand
    {
        AudioClip clip;
        /// <summary>
        /// ���ñ༭����Ƶ����
        /// </summary>
        /// <param name="audioClip">��Ƶ</param>
        public SetEditAudioAudioCommand(AudioClip audioClip)
        {
            clip = audioClip;
        }
        protected override void OnExecute()
        {
            this.GetModel<AudioEditModel>().EditAudioClip = clip;
        }
    }

}

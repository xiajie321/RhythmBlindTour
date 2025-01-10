using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;

namespace Qf.Commands.AudioEdit
{
    /// <summary>
    /// 设置编辑器当前播放时间命令
    /// </summary>
    public class SetAudioEditThisTimeCommand : AbstractCommand
    {
        float value;
        /// <summary>
        /// 设置编辑器当前播放时间命令
        /// </summary>
        /// <param name="time">时间</param>
        public SetAudioEditThisTimeCommand(float time)
        {
            value = time;
        }
        protected override void OnExecute()
        {
            AudioEditModel audioEditModel = this.GetModel<AudioEditModel>();
            if (audioEditModel != null)
            {
                audioEditModel.ThisTime = value;
                if(audioEditModel.ThisTime > audioEditModel.EditAudioClip.length)
                {
                    audioEditModel.ThisTime = audioEditModel.EditAudioClip.length;
                }
                else if(audioEditModel.ThisTime<=0)
                {
                    audioEditModel.ThisTime = 0;
                }
            }
            this.SendEvent<UpdateThisTimeEvent>();
        }
    }
}

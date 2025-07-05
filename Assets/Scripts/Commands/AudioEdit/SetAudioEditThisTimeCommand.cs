using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;
using System;

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
            value = (float)Math.Round(time, 2, MidpointRounding.ToEven);
        }
        protected override void OnExecute()
        {
            AudioEditModel audioEditModel = this.GetModel<AudioEditModel>();
            if (audioEditModel != null)
            {
                if (audioEditModel.EditAudioClip == null) return;

                if (value > audioEditModel.EditAudioClip.length)
                {
                    audioEditModel.ThisTime = audioEditModel.EditAudioClip.length;
                }
                else if ((audioEditModel.ThisTime == 0 && value <= 0) || (float)Math.Round(audioEditModel.ThisTime, 2, MidpointRounding.ToEven) == value)
                {
                    return;
                }
                else if (value <= 0)
                {
                    value = 0;
                    audioEditModel.ThisTime = value;
                }
                else
                {
                    audioEditModel.ThisTime = value;
                }
                value = (float)Math.Round(value, 2, MidpointRounding.ToEven);
                this.SendEvent(new OnUpdateThisTime()
                {
                    ThisTime = value
                });
                this.SendEvent(new OnStartThisTime()
                {
                    ThisTime = value
                });
            }
        }
    }
}

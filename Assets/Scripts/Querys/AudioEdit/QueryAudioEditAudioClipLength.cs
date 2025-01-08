using Qf.Models.AudioEdit;
using QFramework;
namespace Qf.Querys.AudioEdit
{
    /// <summary>
    /// ��ȡ��ǰ��Ƶ�༭����Ƶ����
    /// </summary>
    public class QueryAudioEditAudioClipLength : AbstractQuery<float>
    {
        AudioEditModel audioEditModel;
        protected override float OnDo()
        {
            audioEditModel = this.GetModel<AudioEditModel>();
            if (audioEditModel.EditAudioClip != null)
            {
                return audioEditModel.EditAudioClip.length;
            }
            return 0;
        }
    }
}


//------------------------------------------------------------

using GameFramework;
using GameFramework.DataTable;
using GameFramework.Sound;
using UnityGameFramework.Runtime;

namespace Lokas
{
    //声音工具类，根据需求修改
    //此处有3种音效播放方法
    public static class SoundExtension
    {
        private const float FadeVolumeDuration = 1f;
        private static int? s_MusicSerialId = null;

        /// <summary>
        /// 播放BGM
        /// </summary>
        /// <param name="soundComponent"></param>
        /// <param name="musicId"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public static int? PlayMusic(this SoundComponent soundComponent, int musicId, object userData = null)
        {   //停止上一个背景音乐
            soundComponent.StopMusic();
            //读取Id指定配置
            IDataTable<DRSound> dtMusic = GameEntry.DataTable.GetDataTable<DRSound>();
            DRSound drMusic = dtMusic.GetDataRow(musicId);
            if (drMusic == null)
            {
                Log.Warning("Can not load music '{0}' from data table.", musicId.ToString());
                return null;
            }

            PlaySoundParams playSoundParams = PlaySoundParams.Create();
            playSoundParams.Priority = 64;
            playSoundParams.Loop = true;
            playSoundParams.VolumeInSoundGroup = 1f;
            playSoundParams.FadeInSeconds = FadeVolumeDuration;
            playSoundParams.SpatialBlend = 0f;
            s_MusicSerialId = soundComponent.PlaySound(AssetUtility.GetMusicAsset(drMusic.AssetName), "Music", Constant.AssetPriority.MusicAsset, playSoundParams, null, userData);
            return s_MusicSerialId;
        }

        
        public static void StopMusic(this SoundComponent soundComponent)
        {
            if (!s_MusicSerialId.HasValue)
            {
                return;
            }

            soundComponent.StopSound(s_MusicSerialId.Value, FadeVolumeDuration);
            s_MusicSerialId = null;
        }
        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="soundComponent"></param>
        /// <param name="soundId"></param>
        /// <param name="bindingEntity"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public static int? PlaySound(this SoundComponent soundComponent, int soundId, Entity bindingEntity = null, object userData = null)
        {
            IDataTable<DRSound> dtSound = GameEntry.DataTable.GetDataTable<DRSound>();
            DRSound drSound = dtSound.GetDataRow(soundId);
            if (drSound == null)
            {
                Log.Warning("Can not load sound '{0}' from data table.", soundId.ToString());
                return null;
            }

            PlaySoundParams playSoundParams = PlaySoundParams.Create();
            playSoundParams.Priority = drSound.Priority;
            playSoundParams.Loop = drSound.Loop;
            playSoundParams.VolumeInSoundGroup = drSound.Volume;
            playSoundParams.SpatialBlend = drSound.SpatialBlend;
            return soundComponent.PlaySound(AssetUtility.GetSoundAsset(drSound.AssetName), "Sound", Constant.AssetPriority.SoundAsset, playSoundParams, bindingEntity != null ? bindingEntity.Entity : null, userData);
        }
        /// <summary>
        ///播放UI音效
        /// </summary>
        /// <param name="soundComponent"></param>
        /// <param name="uiSoundId"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public static int? PlayUISound(this SoundComponent soundComponent, int uiSoundId, object userData = null)
        {
            IDataTable<DRSound> dtUISound = GameEntry.DataTable.GetDataTable<DRSound>();
            DRSound drUISound = dtUISound.GetDataRow(uiSoundId);
            if (drUISound == null)
            {
                Log.Warning("Can not load UI sound '{0}' from data table.", uiSoundId.ToString());
                return null;
            }

            PlaySoundParams playSoundParams = PlaySoundParams.Create();
            playSoundParams.Priority = drUISound.Priority;
            playSoundParams.Loop = false;
            playSoundParams.VolumeInSoundGroup = drUISound.Volume;
            playSoundParams.SpatialBlend = 0f;
            return soundComponent.PlaySound(AssetUtility.GetUISoundAsset(drUISound.AssetName), "UISound", Constant.AssetPriority.UISoundAsset, playSoundParams, userData);
        }

        public static bool IsMuted(this SoundComponent soundComponent, string soundGroupName)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                Log.Warning("Sound group is invalid.");
                return true;
            }

            ISoundGroup soundGroup = soundComponent.GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                Log.Warning("Sound group '{0}' is invalid.", soundGroupName);
                return true;
            }

            return soundGroup.Mute;
        }

        public static void Mute(this SoundComponent soundComponent, string soundGroupName, bool mute)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                Log.Warning("Sound group is invalid.");
                return;
            }

            ISoundGroup soundGroup = soundComponent.GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                Log.Warning("Sound group '{0}' is invalid.", soundGroupName);
                return;
            }

            soundGroup.Mute = mute;


        }

        public static float GetVolume(this SoundComponent soundComponent, string soundGroupName)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                Log.Warning("Sound group is invalid.");
                return 0f;
            }

            ISoundGroup soundGroup = soundComponent.GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                Log.Warning("Sound group '{0}' is invalid.", soundGroupName);
                return 0f;
            }

            return soundGroup.Volume;
        }

        public static void SetVolume(this SoundComponent soundComponent, string soundGroupName, float volume)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                Log.Warning("Sound group is invalid.");
                return;
            }

            ISoundGroup soundGroup = soundComponent.GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                Log.Warning("Sound group '{0}' is invalid.", soundGroupName);
                return;
            }

            soundGroup.Volume = volume;


        }
    }
}

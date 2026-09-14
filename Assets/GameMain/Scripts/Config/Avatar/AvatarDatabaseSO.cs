using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    [CreateAssetMenu(menuName = "GF Template/Avatar/Avatar Database", fileName = "AvatarDatabase")]
    public class AvatarDatabaseSO : ScriptableObject
    {
        [Header("Avatars")]
        [SerializeField] private List<AvatarEntrySO> m_Avatars = new();

        [Header("Avatar Frames")]
        [SerializeField] private List<AvatarFrameEntrySO> m_Frames = new();

        private Dictionary<int, AvatarEntrySO> _avatarDic;
        private Dictionary<int, AvatarFrameEntrySO> _frameDic;

        private void InitDics()
        {
            _avatarDic = new Dictionary<int, AvatarEntrySO>();
            foreach (var entry in m_Avatars)
            {
                if (entry != null)
                    _avatarDic[entry.id] = entry;
            }

            _frameDic = new Dictionary<int, AvatarFrameEntrySO>();
            foreach (var entry in m_Frames)
            {
                if (entry != null)
                    _frameDic[entry.id] = entry;
            }
        }

        public bool TryGetAvatar(int id, out AvatarEntrySO entry)
        {
            if (_avatarDic == null) InitDics();
            return _avatarDic.TryGetValue(id, out entry);
        }

        public bool TryGetFrame(int id, out AvatarFrameEntrySO entry)
        {
            if (_frameDic == null) InitDics();
            return _frameDic.TryGetValue(id, out entry);
        }

        public IReadOnlyList<AvatarEntrySO> GetAllAvatars() => m_Avatars;
        public IReadOnlyList<AvatarFrameEntrySO> GetAllFrames() => m_Frames;
    }
}

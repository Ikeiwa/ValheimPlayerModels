using System.Collections;

namespace ValheimPlayerModels.Loaders
{
    public class VrmLoader : AvatarLoaderBase
    {
        public override IEnumerator LoadFile(string file)
        {
            LoadedSuccessfully = false;
            yield break;
        }

        public override AvatarInstance LoadAvatar(PlayerModel playerModel)
        {
            return null;
        }

        public override void Unload()
        {

        }
    }
}
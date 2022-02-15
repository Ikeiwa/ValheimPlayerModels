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

        public override bool LoadAvatar(PlayerModel playerModel)
        {
            return false;
        }

        public override void Unload()
        {

        }

        public override void Destroy()
        {

        }
    }
}
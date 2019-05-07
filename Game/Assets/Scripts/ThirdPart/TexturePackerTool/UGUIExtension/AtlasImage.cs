using UnityEngine;
using UnityEngine.UI;

public class AtlasImage : Image {
	[SerializeField]protected Atlas atlas = null;

	public void SetSpriteByName(string spriteName){
		if(atlas == null){
			return;
		}

		Sprite spriteTarget = atlas.GetSprite(spriteName);
		if(spriteTarget != null){
			this.sprite = spriteTarget;
		}
	}
}
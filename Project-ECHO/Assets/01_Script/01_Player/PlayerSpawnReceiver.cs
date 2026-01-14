using Photon.Pun;
using UnityEngine;

public class PlayerSpawnReceiver : MonoBehaviourPun
{
    [PunRPC]
    public void SetSpawnByIndex(int index)
    {
        Transform sp = CombatSceneSetup.Instance.GetSpawnPoint(index);
        if (sp == null)
        {
            Debug.LogError("[PlayerSpawnReceiver] SpawnPoint가 null입니다.");
            return;
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        transform.SetPositionAndRotation(sp.position, sp.rotation);

        if (cc != null) cc.enabled = true;

        Debug.Log($"[PlayerSpawnReceiver] 스폰 적용 index={index}, pos={sp.position}");
    }
}

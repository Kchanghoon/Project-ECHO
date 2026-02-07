using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button startButton;

    private void Awake()
    {
        hostButton.onClick.AddListener(CreateRoom);
        clientButton.onClick.AddListener(JoinRandomRoom);
        startButton.onClick.AddListener(OnStartRoom);

        hostButton.interactable = false;
        clientButton.interactable = false;
        startButton.interactable = false;
    }

    private void Start()
    {
        Debug.Log($"[Photon] AppIdRealtime={PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime}");

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = "Player_" + Random.Range(0, 1000);
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon 서버 연결 완료");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비 입장 완료");
        hostButton.interactable = true;
        clientButton.interactable = true;
    }

    private void CreateRoom()
    {
        hostButton.interactable = false;
        clientButton.interactable = false;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        PhotonNetwork.CreateRoom("Room_" + Random.Range(0, 10000), roomOptions);
    }

    private void JoinRandomRoom()
    {
        Debug.Log("랜덤 방 입장 시도...");
        clientButton.interactable = false;
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning("방 정보가 동기화되지 않았습니다.");
            return;
        }

        Debug.Log($"방 입장 성공: {PhotonNetwork.CurrentRoom.Name}");

        if (startButton != null)
        {
            startButton.interactable = PhotonNetwork.IsMasterClient;

            if (!PhotonNetwork.IsMasterClient)
            {
                var btnText = startButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (btnText != null) btnText.text = "Waiting for Host...";
            }
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.interactable = true;
        }
    }

    public void OnStartRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.interactable = false;
            PhotonNetwork.LoadLevel("03_Combat");
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("랜덤 방 입장 실패 - 새로운 방 생성");
        CreateRoom();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패: {message}");
    }
}
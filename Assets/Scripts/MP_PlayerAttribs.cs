using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MP_PlayerAttribs : NetworkBehaviour
{
    public Slider hpBar;

    private float maxHp = 100f;
    private float damageVal = 20f;

    private NetworkVariableFloat currentHp = new NetworkVariableFloat(100f);
    public NetworkVariableBool powerUp = new NetworkVariableBool(false);
    [SerializeField] private GameObject playerPrefab;
    public NetworkVariableInt deaths = new NetworkVariableInt(0);
    public NetworkVariableInt kills = new NetworkVariableInt(0);

    // Update is called once per frame
    void Update()
    {
        hpBar.value = currentHp.Value / maxHp;

        if(currentHp.Value < 0)
        {
            RespawnPlayerServerRpc();
            ResetPlayerClientRpc();
            if (IsOwner)
            {
                Debug.Log("Dead.");
            }
        }

    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet") && IsOwner)
        {
            if (collision.gameObject.GetComponent<MP_BulletScript>().spawnerPlayerID != OwnerClientId)
            {
                if(currentHp.Value - damageVal < 0)
                {
                    IncreaseKillCountServerRpc(collision.gameObject.GetComponent<MP_BulletScript>().spawnerPlayerID);
                }

                TakeDamageServerRpc(damageVal);
                Destroy(collision.gameObject);
            }
            
        }
        else if (collision.gameObject.CompareTag("MedKit") && IsOwner)
        {
            Debug.Log("I am Healed");
            HealDamageServerRpc();
        }
        else if (collision.gameObject.CompareTag("PowerUp") && IsOwner)
        {
            Debug.Log("I have the power!");
            damageVal = damageVal / 2;
            powerUp.Value = true;
        }
    }

    [ServerRpc]
    private void TakeDamageServerRpc(float damage, ServerRpcParams svrParams = default)
    {
        currentHp.Value -= damage;
        if(currentHp.Value < 0)
        {
            if (currentHp.Value < 0 && OwnerClientId == svrParams.Receive.SenderClientId)
            {
                deaths.Value++;
            }
        }
    }

    [ServerRpc]
    private void HealDamageServerRpc()
    {
        currentHp.Value += 25f;
        if(currentHp.Value > maxHp)
        {
            currentHp.Value = maxHp;
        }

    }
    [ServerRpc]

    private void RespawnPlayerServerRpc()
    {
        currentHp.Value = maxHp;
    }
    [ClientRpc]
    private void ResetPlayerClientRpc()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        int index = UnityEngine.Random.Range(0, spawnPoints.Length);

        GetComponent<CharacterController>().enabled = false;
        transform.position = spawnPoints[index].transform.position;
        GetComponent<CharacterController>().enabled = true;
    }
    [ServerRpc]

    private void IncreaseKillCountServerRpc(ulong spawnerPlayerID)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach(GameObject playerObj in players)
        {
            if(playerObj.GetComponent<NetworkObject>().OwnerClientId == spawnerPlayerID)
            {
                playerObj.GetComponent<MP_PlayerAttribs>().kills.Value++;
            }
        }
    }
}

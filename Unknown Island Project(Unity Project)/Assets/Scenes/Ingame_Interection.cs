﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;
using Mono.Data.Sqlite;
using Assets.Scenes;

public class Ingame_Interection : MonoBehaviour
{
    static private List<GameObject> tree_log_list;
    static private List<GameObject> tree_fruit_list;
    static private List<GameObject> fishtrap_list;
    static private List<int> fishtrap_waitcount_list;//통발이 기다린 시간에 따라 +1 씩 증가하니까 그에 따른 물고기 양도 확률 적으로 증가하게 하셈
    static private List<bool> fishtrap_wait_bool_list;

    static private bool fishtrap_count_already;

    public GameObject fishtrap_text;
    public GameObject setting_point;
    public GameObject fishtrap_overlap;

    private UKI_script uki;
    public Ingame_Interection()
    {
        tree_log_list = new List<GameObject>();
        tree_fruit_list = new List<GameObject>();
        fishtrap_list = new List<GameObject>();
        fishtrap_waitcount_list = new List<int>();
        fishtrap_wait_bool_list = new List<bool>();
        fishtrap_text = GameObject.Find("Fishtrap_isFull_Text");
        fishtrap_text.SetActive(false);
        setting_point = GameObject.Find("Setting_point");
        fishtrap_overlap = GameObject.Find("Fish_trap_overlap");
        fishtrap_overlap.SetActive(false);

        fishtrap_count_already = false;

        uki = new UKI_script();
    }

    void Start()
    {

    }

    void Update()
    {

    }
    ///<summary>
    ///유저가 나무를 향해 화면을 보고 있는지 판단해서 press E 이미지를 띄우고 그 상태에서 E 누르면 나무를 캐고(비활성화) 하위 아이템을 해당 좌표에 활성화 후 순간이동 시키는 함수
    ///</summary>
    public void RayCastTree(Transform charactor, GameObject press_tree_image, string[] key_custom_arry, GameObject tree_log, GameObject tree_fruit, LayerMask laymask_tree)
    {
        //if(도끼를 장착 했는지 확인)
        RaycastHit hitinfo;
        Debug.DrawRay(charactor.position - charactor.forward, charactor.forward, Color.yellow, 2f);
        if (Physics.SphereCast(charactor.position - charactor.forward, 1f, charactor.forward, out hitinfo, 2f, laymask_tree))
        {
            press_tree_image.SetActive(true);
            if (Input.GetKeyDown(key_custom_arry[1]))
            {
                //나무 패는 에니메이션

                TreeItemCreate(hitinfo.collider.gameObject.transform, tree_log, tree_fruit);


                hitinfo.collider.gameObject.SetActive(false);
            }
        }
        else if(press_tree_image.activeSelf)
        {
            press_tree_image.SetActive(false);
        }
    }
    ///<summary>
    ///유저가 나무하위 아이템을 향해 화면을 보고 있는지 판단해서 press E 이미지를 띄우고 그 상태에서 E 누르면 아이템을 줍고(비활성화) 아이템을 저장하는 함수
    ///</summary>
    public void RayCastTreeItem(Transform charactor, GameObject press_treeitem_image, string[] key_custom_arry, LayerMask laymask_tree_item)
    {
        RaycastHit hitinfo;
        if (Physics.SphereCast(charactor.position - charactor.forward, 1f, charactor.forward, out hitinfo, 2f, laymask_tree_item))
        {
            press_treeitem_image.SetActive(true);
            if (Input.GetKeyDown(key_custom_arry[1]))
            {
                if(hitinfo.collider.gameObject.tag == "Tree_Log")
                {
                    //줍는 에니메이션
                    //인벤토리 데이터 베이스에 통나무 저장
                    Destroy(hitinfo.collider.gameObject);
                }
                else if (hitinfo.collider.gameObject.tag == "Tree_Fruit")
                {
                    //줍는 에니메이션
                    //인벤토리 데이터 베이스에 과일 저장
                    Destroy(hitinfo.collider.gameObject);
                }
                hitinfo.collider.gameObject.SetActive(false);
            }
        }
        else if (press_treeitem_image.activeSelf)
        {
            press_treeitem_image.SetActive(false);
        }
    }
    ///<summary>
    ///나무 재생성 하는 함수
    ///</summary>
    public IEnumerator ResetTree(List<GameObject> tree_list, WaitForSeconds wait, int i)
    {
        i = UnityEngine.Random.Range(0, tree_list.Count);
        if (tree_list[i].activeSelf) { }
        else
        {
            yield return wait;
            if (CheckScreenOut(tree_list[i]))
            {
                //나무가 크는 에니메이션 넣고
                tree_list[i].SetActive(true);
            }
            else
            {
                tree_list[i].SetActive(true);
            }
        }
    }
    ///<summary>
    ///나무가 카메라에 보이는지 확인 하는 함수 보이면 true
    ///</summary>
    private bool CheckScreenOut(GameObject tree)
    {
        Vector3 targetScreenPos = Camera.main.WorldToViewportPoint(tree.transform.position);
        if (targetScreenPos.z >0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    ///<summary>
    ///나무 하위 아이템 생성 하는 함수
    ///</summary>
    private void TreeItemCreate(Transform target, GameObject tree_log, GameObject tree_fruit)
    {
        GameObject log = Instantiate(tree_log, target.position + new Vector3(0, 2.5f, 0), Quaternion.identity);
        GameObject fruit = Instantiate(tree_fruit, target.position + new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), 3.0f, UnityEngine.Random.Range(-1.0f, 1.0f)), Quaternion.identity);
        tree_log_list.Add(log);
        tree_fruit_list.Add(fruit);
        Destroy(log, 30f);
        Destroy(fruit, 30f);
    }
    ///<summary>
    ///물인지 판단해서 통발 설치 및 회수
    ///</summary>
    public void RayCastWaterFishTrap(Transform charactor, GameObject press_water_image, GameObject press_fishtrap_image, string[] key_custom_arry, GameObject fish_trap, LayerMask laymask_water, LayerMask laymask_fishtrap)
    {
        RaycastHit hitinfo_trap;
        //통발 회수
        if (Physics.SphereCast(charactor.position - charactor.forward, 1f, charactor.forward, out hitinfo_trap, 2f, laymask_fishtrap))
        {
            press_fishtrap_image.SetActive(true);
            if (Input.GetKeyDown(key_custom_arry[1]))
            {
                //통발 줍는 애니메이션
                //인벤토리에 통발과 물고기 저장
                int rnd = UnityEngine.Random.Range(1, 101);
                switch (fishtrap_waitcount_list[fishtrap_list.IndexOf(hitinfo_trap.collider.gameObject)])
                {//통발 시간에 따른 물고기 회수량 확률 적용
                    case 0:
                        if(rnd < 95)
                        {
                            //물고기 0개
                        }
                        else
                        {
                            //물고기 1개
                        }
                        break;
                    case 1:
                        if (rnd < 50)
                        {
                            //물고기 1개
                        }
                        else
                        {
                            //물고기 2개
                        }
                        break;
                    case 2:
                        if (rnd < 15)
                        {
                            //물고기 1개
                        }
                        else if (rnd < 50)
                        {
                            //물고기 2개
                        }
                        else
                        {
                            //물고기 3개
                        }
                        break;
                    case 3:
                        if (rnd < 5)
                        {
                            //물고기 1개
                        }
                        else if (rnd < 30)
                        {
                            //물고기 2개
                        }
                        else if (rnd < 60)
                        {
                            //물고기 3개
                        }
                        else
                        {
                            //물고기 4개
                        }
                        break;
                    case 4:
                        if (rnd < 5)
                        {
                            //물고기 2개
                        }
                        else if (rnd < 30)
                        {
                            //물고기 3개
                        }
                        else if (rnd < 60)
                        {
                            //물고기 4개
                        }
                        else
                        {
                            //물고기 5개
                        }
                        break;
                    default:
                        if (rnd < 5)
                        {
                            //물고기 3개
                        }
                        else if (rnd < 30)
                        {
                            //물고기 4개
                        }
                        else if (rnd < 60)
                        {
                            //물고기 5개
                        }
                        else
                        {
                            //물고기 6개
                        }
                        break;
                }
                fishtrap_waitcount_list.RemoveAt(fishtrap_list.IndexOf(hitinfo_trap.collider.gameObject));
                fishtrap_wait_bool_list.RemoveAt(fishtrap_list.IndexOf(hitinfo_trap.collider.gameObject));
                fishtrap_list.Remove(hitinfo_trap.collider.gameObject);
                Destroy(hitinfo_trap.collider.gameObject);
            }
        }
        else if (press_fishtrap_image.activeSelf)
        {
            press_fishtrap_image.SetActive(false);
        }
        //통발 설치
        //if(통발을 장착 했는지 확인)
        else if (Physics.Raycast(charactor.position + charactor.up * 5f, -charactor.up, 5.2f, laymask_water))
        {
            press_water_image.SetActive(true);
            if (fishtrap_list.Count < 4)
            {
                fishtrap_overlap.SetActive(true);
                fishtrap_overlap.transform.position = setting_point.transform.position;
            }
            else
            {
                fishtrap_overlap.SetActive(false);
            }
            if (Input.GetKeyDown(key_custom_arry[1]))
            {
                if (fishtrap_list.Count < 4)
                {
                    fishtrap_overlap.SetActive(false);
                    FishTrapCreate(setting_point.transform.position, fish_trap);
                }
                else
                {
                    if(!fishtrap_text.activeSelf)
                    {
                        fishtrap_count_already = true;
                    }
                }
            }
        }
        else if (press_water_image.activeSelf)
        {
            press_water_image.SetActive(false);
            fishtrap_overlap.SetActive(false);
        }
    }
    ///<summary>
    ///통발 최대 갯수 텍스트 체크 해서 시간 지나면 해제 하는 함수
    ///</summary>
    public IEnumerator FishtrapIsfull(WaitForSeconds wait, WaitForFixedUpdate wait_fix)
    {
        yield return wait_fix;
        if (fishtrap_count_already)
        {
            fishtrap_count_already = false;
            fishtrap_text.SetActive(true);
            yield return wait;
            fishtrap_text.SetActive(false);
        }
    }
    ///<summary>
    ///통발 클론 생성 함수
    ///</summary>
    private void FishTrapCreate(Vector3 tr_point, GameObject fish_trap)
    {
        GameObject ft = Instantiate(fish_trap, tr_point, Quaternion.identity);
        fishtrap_list.Add(ft);
        fishtrap_waitcount_list.Add(0);
        fishtrap_wait_bool_list.Add(false);
    }
    ///<summary>
    ///통발 설치 한지 얼마나 지났는지 확인 해주는 함수
    ///</summary>
    public IEnumerator CountFishTrap(WaitForSeconds wait, AsyncOperation wait_settingtrap, int i)
    {
        yield return wait_settingtrap;
        if (fishtrap_list.Count > 0)
        {
            i = UnityEngine.Random.Range(0, fishtrap_list.Count);
            if (fishtrap_list[i] == null) { }
            else if(fishtrap_wait_bool_list[i] == false)
            {
                fishtrap_wait_bool_list[i] = true;
                yield return wait;
                try
                {
                    fishtrap_waitcount_list[i]++;
                    fishtrap_wait_bool_list[i] = false;
                }
                catch(Exception ex)
                {

                }
            }
        }
    }
    ///<summary>
    ///돌 캐는 함수
    ///</summary>
    public void RayCastRock(Transform charactor, GameObject press_rock_image, GameObject press_stone_image, string[] key_custom_arry, GameObject rock_stone, LayerMask laymask_rock, LayerMask laymask_stone)
    {
        //if(곡괭이를 장착 했는지 확인)
        RaycastHit hitinfo;
        if (Physics.SphereCast(charactor.position - charactor.forward, 1f, charactor.forward, out hitinfo, 2f, laymask_rock))
        {
            press_rock_image.SetActive(true);
            if (Input.GetKeyDown(key_custom_arry[1]))
            {
                //곡괭이질 애니메이션
                RockItemCreate(hitinfo.point, hitinfo.collider.gameObject.transform.position, rock_stone);
            }
        }
        else if (press_rock_image.activeSelf)
        {
            press_rock_image.SetActive(false);
        }
        else if (Physics.SphereCast(charactor.position - charactor.forward, 1f, charactor.forward, out hitinfo, 2f, laymask_stone))
        {
            press_stone_image.SetActive(true);
            if (Input.GetKeyDown(key_custom_arry[1]))
            {
                //줍는 애니메이션 및 줍기 구현
            }
        }
        else if(press_stone_image.activeSelf)
        {
            press_stone_image.SetActive(false);
        }
    }
    ///<summary>
    ///돌 하위 아이템 생성 함수
    ///</summary>
    private void RockItemCreate(Vector3 item_point, Vector3  tr_point, GameObject rock_stone)
    {
        Destroy(Instantiate(rock_stone, item_point - tr_point.normalized, Quaternion.identity), 30f);
    }
}

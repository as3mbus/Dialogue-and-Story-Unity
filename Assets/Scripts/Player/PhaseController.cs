﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using as3mbus.Story;

public class PhaseController : MonoBehaviour
{
    public Transform kameraRoute, kamera, baloonPos, baloonsizer;
    public Text dName, dText;
    public GameObject dPanel;
    int currentLine = 0, currentChar = 0;
    public SpriteRenderer pageL, pageR;
    public float speed = 5f, routeRadius = 1f, typeDelay = 0.2f;
    public float duration;

    public float times;
    float timeCount;
    Phase activePhase;
    private StoryController ssControl;
    Vector3 originPosition;
    float originZoom;
    bool movingCamera;
    public bool pageLR = true;
    public float shake = 0.2f;

    //Start phase by setting active phase and load phase 
    public void startPhase(Phase fase)
    {
        this.activePhase = fase;
        // Debug.Log(fase.toJson());
        currentLine = 0;
        readLine(currentLine);
    }
    // Update is called once per frame
    void Start()
    {
        //access story controller 
        ssControl = FindObjectOfType<StoryController>();
    }
 
    void Update()
    {
        if (currentLine >= activePhase.messages.Count) return;
        //read fire 1 button pressed  
        if (Input.GetButtonDown("Fire1"))
        {
            //skip transition 
            times = duration;
            spriteFade(activePhase.fademode[currentLine]);
            camRoute();

            //if line finished reading 
            // read next line or hide phase (complete phase) 
            if (currentChar >= activePhase.messages[currentLine].Length)
            {
                //if there are more line after current line
                //read next line 
                if (currentLine < activePhase.messages.Count - 1)
                {
                    currentLine++;
                    readLine(currentLine);
                }
                //hide / complete the phase 
                else
                {
                    hidePhase();
                }
            }
            // read complete line
            else
            {
                showLine(activePhase.messages[currentLine]);
            }
        }
        //fade transition 
        spriteFade(activePhase.fademode[currentLine]);
        //text per sec 
        textPerSec(typeDelay);
        //if not using color fade
        //slowly move camera position (pan and zoom) 
        if (activePhase.fademode[currentLine] != fadeMode.color)
            camRoute();
        //shake camera
        shakeCamera(activePhase.shake[currentLine], shake);
        //show talking baloon
        showBaloon();
    }
    //read complete line 
    public void showLine(string line)
    {
        dText.text = line;
        currentChar = line.Length;
    }
    //show talking baloon 
    public void showBaloon()
    {
        if (times >= duration && !baloonPos.gameObject.activeSelf)
        {
            baloonPos.gameObject.SetActive(true);
            baloonPos.GetComponent<Animation>().Play();
        }
    }
    //read every data about the line and use it to make player interface/ looks
    public void readLine(int line)
    {
        if (line >= activePhase.messages.Count) return;
        //swapping page on fademode and loading it 
        if (activePhase.fademode[line] != fadeMode.none)
        {
            pageLR = !pageLR;
            activePage().color = new Color(1, 1, 1, 0);
            activePage().sprite = activePhase.comic.pages[activePhase.pages[currentLine]];
        }
        baloonsizer.transform.localScale = new Vector2(activePhase.baloonsize[line], activePhase.baloonsize[line]);
        originPosition = kameraRoute.position;
        originZoom = kamera.GetComponent<Camera>().orthographicSize;
        times = 0;
        duration = activePhase.duration[line];
        currentChar = 0;
        dPanel.SetActive(false);
        dName.text = activePhase.characters[line];
        dText.text = "";
        kamera.GetComponent<Camera>().backgroundColor = activePhase.bgcolor[line];

        if (
            Mathf.Abs(activePhase.baloonpos[line].x)
             + Mathf.Abs(activePhase.baloonpos[line].y)
             != 0)
        {
            baloonPos.gameObject.SetActive(true);
            baloonPos.localPosition = activePhase.baloonpos[line];
            baloonPos.gameObject.SetActive(false);
        }
        else
            baloonPos.gameObject.SetActive(false);
    }
    //type a character after a delay 
    public void textPerSec(float delay)
    {
        if (times < duration)
            return;
        dPanel.SetActive(activePhase.messages[currentLine] != "");
        if (currentChar >= activePhase.messages[currentLine].Length)
            return;

        timeCount += Time.deltaTime;
        if (timeCount > delay)
        {
            dText.text = dText.text + activePhase.messages[currentLine][currentChar];
            currentChar++;
            timeCount = 0;
        }
    }
    //fading effect
    void spriteFade(fadeMode fadeM)
    {
        if (times < duration)
            times += Time.deltaTime;
        if (fadeM == fadeMode.color)
            colorFade();
        else if (fadeM == fadeMode.transition)
            transitionFade();
    }
    //Transition Fade 
    void transitionFade()
    {
        inactivePage().color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), times / duration);
        activePage().color = Color.Lerp(Color.white, new Color(1, 1, 1, 1), times / duration);
    }
    //color fade 
    void colorFade()
    {
        if (times < duration / 2)
        {
            inactivePage().color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), (times * 2) / duration);
        }
        else
        {
            camPos();
            activePage().color = Color.Lerp(new Color(1, 1, 1, 0), Color.white, (times * 2 - duration) / duration);
        }
    }
    // shake camera 
    void shakeCamera(float frequency, float magnitude)
    {
        Vector2 shakeVector;
        float seed = Time.time * frequency;
        // print(Time.time + " * " + frequency + " = " + seed);
        // print("Perlin = " + Mathf.PerlinNoise(seed, 0f));
        shakeVector.x = Mathf.PerlinNoise(seed, 0f) - 0.5f;
        shakeVector.y = Mathf.PerlinNoise(0f, seed) - 0.5f;
        shakeVector = shakeVector * magnitude;
        kamera.localPosition = shakeVector;

    }
    //accessing active page sprite 
    public SpriteRenderer activePage()
    {
        return pageLR ? pageL : pageR;
    }
    //accessing inactive page sprite 
    public SpriteRenderer inactivePage()
    {
        return !pageLR ? pageL : pageR;
    }
    //moving camera to designated point 
    public void camRoute()
    {
        float distance = Vector3.Distance(activePhase.paths[currentLine], kameraRoute.position);
        float zoomDistance = Mathf.Abs(kamera.GetComponent<Camera>().orthographicSize - activePhase.zooms[currentLine]);
        if (distance != 0)
            kameraRoute.position = Vector3.MoveTowards(originPosition, activePhase.paths[currentLine], times / duration);
        if (zoomDistance != 0)
            kamera.GetComponent<Camera>().orthographicSize = Mathf.Lerp(originZoom, activePhase.zooms[currentLine], times / duration);

        // if (Input.GetButtonDown("Fire1") && currentLine < activePhase.paths.Count)
        // {
        //     currentLine++;
        // }
    }
    //set camera position
    public void camPos()
    {
        kameraRoute.position = activePhase.paths[currentLine];
    }
    //hide phase and call story controller next phase
    public void hidePhase()
    {
        pageLR = true;
        pageL.color = Color.white;
        pageR.color = Color.white;
        gameObject.SetActive(false);
        ssControl.nextPhase();
    }
}

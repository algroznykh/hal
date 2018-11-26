using System;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Net;
using Newtonsoft.Json;


public struct RecognitionRequest
{
	/// <summary>
	/// structs for google cloud speech recognition
	/// https://cloud.google.com/speech-to-text/docs/reference/rest/v1/speech/recognize
	/// </summary>
	///
	public RecognitionConfig config;
	public RecognitionAudio audio;
}


public struct RecognitionConfig 
{
	public int sampleRateHertz;

	public string languageCode;
	
}


public struct RecognitionAudio
{
	public string content;
}


public struct RecognitionAlternative
{
	public string transcript;
	public float confidence;
}


public struct RecognitionResults
{
	public RecognitionAlternative[] alternatives;
}


public struct RecognitionResponse
{
	public RecognitionResults[] results;
}


[RequireComponent(typeof(AudioSource))]
public class SpeechRecognizer : MonoBehaviour
{
	public Text uiText;
	public bool TriggerUp;
	public bool TriggerDown;

	public int minFreq;
	public int maxFreq;

	public AudioSource goAudioSource;

	public string apiKey;
	public string apiUrl;
	
	public RecognitionResponse Response;


	void Start()
	{
		uiText = GameObject.Find("Text").GetComponent<Text>();

		Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);
		Debug.Log("Mic freqs: " + minFreq.ToString() + maxFreq.ToString());

		goAudioSource = GameObject.Find("Asource").GetComponent<AudioSource>();

		apiKey = null; // Register on google cloud to get API key for speech recognition
		
		apiUrl = "https://speech.googleapis.com/v1/speech:recognize?key=" + apiKey;
	}

	void Update()
		{
			TriggerUp = OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger);
			TriggerDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);

			if (TriggerDown)
			{
				uiText.text = "I'm listening...";
				goAudioSource.clip = Microphone.Start(null, true, 7, maxFreq);
			}

			if (TriggerUp)
			{
				Microphone.End(null);

				uiText.text = "processing...";

				float filenameRand = UnityEngine.Random.Range(0.0f, 10.0f);
				string filename = "testing" + filenameRand;


				if (!filename.ToLower().EndsWith(".wav"))
				{
					filename += ".wav";
				}

				var filePath = Path.Combine("testing/", filename);
				filePath = Path.Combine(Application.persistentDataPath, filePath);
				Debug.Log("Created filepath string: " + filePath);

				// Make sure directory exists if user is saving to sub dir.
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				SavWav.Save(filePath, goAudioSource.clip); //Save a temporary Wav File
				Debug.Log("Saving @ " + filePath);


				RecognitionConfig recognitionConfig = new RecognitionConfig();

				recognitionConfig.sampleRateHertz = maxFreq;
				recognitionConfig.languageCode = "en-US";

				//Encode audio to bytes for transmission to google cloud
				Byte[] bytes = File.ReadAllBytes(filePath);
				String fileContent = Convert.ToBase64String(bytes);

				RecognitionAudio recognitionAudio = new RecognitionAudio();
				recognitionAudio.content = fileContent;

				RecognitionRequest recognitionRequest = new RecognitionRequest();
				recognitionRequest.config = recognitionConfig;
				recognitionRequest.audio = recognitionAudio;

				string recognitionRequestJSON = JsonConvert.SerializeObject(recognitionRequest);

				Debug.Log("Making request to " + apiUrl);

				HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(apiUrl);
				webRequest.ContentType = "application/json";
				webRequest.Method = "POST";
				webRequest.KeepAlive = true;

				using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
				{
					streamWriter.Write(recognitionRequestJSON);
					streamWriter.Flush();
					streamWriter.Close();
				}

				var webResponse = (HttpWebResponse) webRequest.GetResponse();

				using (var reader = new StreamReader(webResponse.GetResponseStream()))
				{
					var response_text = reader.ReadToEnd();
					Debug.Log(response_text);
					Response = JsonConvert.DeserializeObject<RecognitionResponse>(response_text);
				}

				Debug.Log("Response:");
				Debug.Log(Response.ToString());

				var guess = Response.results[0].alternatives[0].transcript;
				uiText.text = guess;
				goAudioSource.PlayOneShot((AudioClip) Resources.Load("imsorrydave"));
			}
		}
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using SIGVerse.Common;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviMenu : MonoBehaviour//, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		[HeaderAttribute("Panels")]
		public GameObject mainPanel;
		public GameObject giveUpPanel;
		public GameObject scorePanel;
		public GameObject startTaskPanel;
		public GameObject goToNextTrialPanel;
		public GameObject noticePanel;

		[HeaderAttribute("Text")]
		public Text noticeText;

		[HeaderAttribute("Moderator")]
		public HumanNaviModerator moderator;

		[HeaderAttribute("Camera")]
		public Camera renderCamera;

		//---------------------------------------------------
		private GameObject draggingPanel;

		private Image mainPanelImage;
		private GameObject targetsOfHiding;

		private bool isMainPanelVisible;
		private bool isGiveUpPanelVisible;
		private bool isScorePanelVisible;


		void OnEnable()
		{
			this.mainPanelImage = this.mainPanel.GetComponent<Image>();

			this.targetsOfHiding = this.mainPanel.transform.Find("TargetsOfHiding").gameObject;

			this.mainPanel.SetActive(true);
			this.giveUpPanel.SetActive(false);
			this.scorePanel.SetActive(true);
			this.startTaskPanel.SetActive(false);
			this.goToNextTrialPanel.SetActive(false);
			this.noticePanel.SetActive(false);
		}

		// Update is called once per frame
		void Update()
		{
		}

		public void ClickHiddingButton()
		{
			if (this.mainPanelImage.enabled)
			{
				this.isMainPanelVisible = this.mainPanelImage.enabled;
				this.isGiveUpPanelVisible = this.giveUpPanel.activeSelf;
				this.isScorePanelVisible = this.scorePanel.activeSelf;

				this.mainPanelImage.enabled = false;
				this.targetsOfHiding.SetActive(false);
				this.giveUpPanel.SetActive(false);
				this.scorePanel.SetActive(false);
			}
			else
			{
				if (this.isMainPanelVisible)
				{
					this.mainPanelImage.enabled = true;
					this.targetsOfHiding.SetActive(true);
				}
				if (this.isGiveUpPanelVisible)
				{
					this.giveUpPanel.SetActive(true);
				}
				if (this.isScorePanelVisible)
				{
					this.scorePanel.SetActive(true);
				}
			}
		}

		public void ClickShowGiveUpButton()
		{
			if (this.giveUpPanel.activeSelf)
			{
				this.giveUpPanel.SetActive(false);
			}
			else
			{
				this.giveUpPanel.SetActive(true);
			}
		}

		public void ClickGiveUpYesButton()
		{
			this.moderator.GiveUp();

			if (this.giveUpPanel.activeSelf)
			{
				this.giveUpPanel.SetActive(false);
			}
		}

		public void ClickGiveUpNoButton()
		{
			if (this.giveUpPanel.activeSelf)
			{
				this.giveUpPanel.SetActive(false);
			}
		}

		public void ShowStartTaskPanel()
		{
			this.startTaskPanel.SetActive(true);
		}

		public void ShowGoToNextPanel()
		{
			this.goToNextTrialPanel.SetActive(true);
		}

		public void ClickGoToNextPanel()
		{
			this.moderator.GoToNextTrial();
			this.goToNextTrialPanel.SetActive(false);
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (eventData.pointerEnter == null) { return; }

			Transform selectedObj = eventData.pointerEnter.transform;

			do
			{
				if (selectedObj.gameObject.GetInstanceID() == this.mainPanel.GetInstanceID() ||
					selectedObj.gameObject.GetInstanceID() == this.scorePanel.GetInstanceID() ||
					selectedObj.gameObject.GetInstanceID() == this.giveUpPanel.GetInstanceID())
				{
					this.draggingPanel = selectedObj.gameObject;
					break;
				}

				selectedObj = selectedObj.transform.parent;

			} while (selectedObj.transform.parent != null);
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (this.draggingPanel == null) { return; }

			if (this.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay)
			{
				this.draggingPanel.transform.position += (Vector3)eventData.delta;
			}
			else if (this.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceCamera)
			{
				Vector2 localPos = Vector2.zero;
				Vector2 localPanelPos = RectTransformUtility.WorldToScreenPoint(this.renderCamera, this.draggingPanel.transform.position);
				RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), localPanelPos + eventData.delta, this.renderCamera, out localPos);
				this.draggingPanel.transform.position = this.transform.TransformPoint(localPos);
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			this.draggingPanel = null;
		}
	}
}

using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RoadTransitionManager.GUI {
    public class UIAutoSizePanel :UIPanel{
        public override void Awake() {
            base.Awake();

            // default values can be overriden.
            autoLayout = true;
            autoSize = true; 
            autoLayoutDirection = LayoutDirection.Vertical;
            name = "UIAutoSizePanel"; 
            atlas = TextureUtil.GetAtlas("Ingame"); 
        }

        private bool m_AutoSize;
        public override bool autoSize {
            get {
                return m_AutoSize;
            }
            set {
                m_AutoSize = value;
                Invalidate();
            }
        }

        private void RefreshSize() {
            float widthAcc = 0f;
            if (this.autoLayoutStart.StartsAtLeft()) {
                widthAcc = (float)this.padding.left + (float)this.autoLayoutPadding.left;
            } else if (this.autoLayoutStart.StartsAtRight()) {
                widthAcc = base.size.x - (float)this.padding.right - (float)this.autoLayoutPadding.right;
            }
            float heightAcc = 0f;
            if (!this.useCenter) {
                if (this.autoLayoutStart.StartsAtTop()) {
                    heightAcc = (float)this.padding.top + (float)this.autoLayoutPadding.top;
                } else if (this.autoLayoutStart.StartsAtBottom()) {
                    heightAcc = base.height + (float)this.padding.bottom + (float)this.autoLayoutPadding.bottom;
                }
            }
            float maxWidth = 0f;
            float maxHeight = 0f;
            for (int i = 0; i < base.childCount; i++) {
                UIComponent uicomponent = null;
                if (this.autoLayoutStart.StartsAtLeft()) {
                    uicomponent = this.m_ChildComponents[i];
                } else if (this.autoLayoutStart.StartsAtRight()) {
                    uicomponent = this.m_ChildComponents[base.childCount - 1 - i];
                }
                if (uicomponent.isVisible && uicomponent.enabled && uicomponent.gameObject.activeSelf) {
                    if (!this.useCenter && this.wrapLayout) {
                        if (this.autoLayoutDirection == LayoutDirection.Horizontal) {
                            if (widthAcc + uicomponent.width >= base.size.x - (float)this.padding.right) {
                                if (this.autoLayoutStart.StartsAtLeft()) {
                                    widthAcc = (float)this.padding.left + (float)this.autoLayoutPadding.left;
                                } else if (this.autoLayoutStart.StartsAtRight()) {
                                    widthAcc = base.size.x - (float)this.padding.right - (float)this.autoLayoutPadding.right;
                                }
                                if (this.autoLayoutStart.StartsAtTop()) {
                                    heightAcc += maxHeight;
                                } else if (this.autoLayoutStart.StartsAtBottom()) {
                                    heightAcc -= maxHeight;
                                }
                                maxHeight = 0f;
                            }
                        } else if (heightAcc + uicomponent.height + (float)this.autoLayoutPadding.vertical >= base.size.y - (float)this.padding.bottom) {
                            if (this.autoLayoutStart.StartsAtTop()) {
                                heightAcc = (float)this.padding.top + (float)this.autoLayoutPadding.top;
                            } else if (this.autoLayoutStart.StartsAtBottom()) {
                                heightAcc = base.height + (float)this.padding.bottom + (float)this.autoLayoutPadding.bottom;
                            }
                            if (this.autoLayoutStart.StartsAtLeft()) {
                                widthAcc += maxWidth;
                            } else if (this.autoLayoutStart.StartsAtRight()) {
                                widthAcc -= maxWidth;
                            }
                            maxWidth = 0f;
                        }
                    }
                    Vector2 pos = Vector2.zero;
                    if (this.autoLayoutStart.StartsAtLeft()) {
                        if (this.useCenter) {
                            pos = new Vector2(widthAcc, uicomponent.relativePosition.y);
                        } else if (this.autoLayoutStart.StartsAtTop()) {
                            pos = new Vector2(widthAcc, heightAcc);
                        } else if (this.autoLayoutStart.StartsAtBottom()) {
                            pos = new Vector2(widthAcc, heightAcc - uicomponent.height);
                        }
                    } else if (this.autoLayoutStart.StartsAtRight()) {
                        if (this.useCenter) {
                            pos = new Vector2(widthAcc - uicomponent.width, uicomponent.relativePosition.y);
                        } else if (this.autoLayoutStart.StartsAtTop()) {
                            pos = new Vector2(widthAcc - uicomponent.width, heightAcc);
                        } else if (this.autoLayoutStart.StartsAtBottom()) {
                            pos = new Vector2(widthAcc - uicomponent.width, heightAcc - uicomponent.height);
                        }
                    }
                    float currrentWidth = uicomponent.width + (float)this.autoLayoutPadding.horizontal;
                    float currentHeight = uicomponent.height + (float)this.autoLayoutPadding.vertical;
                    maxWidth = Mathf.Max(currrentWidth, maxWidth);
                    maxHeight = Mathf.Max(currentHeight, maxHeight);
                    if (this.autoLayoutDirection == LayoutDirection.Horizontal) {
                        if (this.autoLayoutStart.StartsAtLeft()) {
                            widthAcc += currrentWidth;
                        } else if (this.autoLayoutStart.StartsAtRight()) {
                            widthAcc -= currrentWidth;
                        }
                    } else if (this.autoLayoutStart.StartsAtTop()) {
                        heightAcc += currentHeight;
                    } else if (this.autoLayoutStart.StartsAtBottom()) {
                        heightAcc -= currentHeight;
                    }
                }
            }
            if(autoLayoutDirection == LayoutDirection.Horizontal) {
                if (autoLayoutStart.StartsAtLeft())
                    widthAcc += padding.right;
                else
                    widthAcc -= padding.left;
                width = widthAcc;
            } else {
                if (autoLayoutStart.StartsAtTop())
                    heightAcc += padding.bottom;
                else
                    heightAcc -= padding.top;
                height = heightAcc;
            }
        }

        public override void Update() {
            base.Update();
            foreach (UIAutoSizePanel panel in this.GetComponents<UIAutoSizePanel>()) {
                panel.Update();
            }
            if(this.m_IsComponentInvalidated && this.autoLayout && base.isVisible) {
                RefreshSize();
            }
        }
    }
}
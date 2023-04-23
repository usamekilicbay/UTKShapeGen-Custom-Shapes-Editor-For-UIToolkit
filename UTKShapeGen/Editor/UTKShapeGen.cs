using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UTKShape;

namespace UTK
{
    public class UTKShapeGen : EditorWindow
    {
        public UTKPropSettings prop;
        public UTKScriptableShapeGen instance;
        private VisualElement canvas;
        private VisualElement tab;
        private List<VisualElement> containers = new();
        private VisualElement rightPanel;
        private VisualElement leftPanel;
        private List<VisualElement> roots = new();
        private VisualElement previewWindow;
        private Image previewImage;
        private VisualElement browserContainer;
        private void DrawPreviewOverlay(bool show)
        {
            if(show)
            {
                if(browserContainer != null)
                    browserContainer.Clear();

                previewWindow = new VisualElement().Size(100, 100, true, true).FlexGrow(1).Position(Position.Absolute).BcgColor(Color.white);
                var img = new Image().Size(100, 100, true, true).FlexGrow(1);
                img.scaleMode = ScaleMode.ScaleToFit;
                previewWindow.AddChild(img);
                previewImage = img;
                rightPanel.AddChild(previewWindow);
                browserContainer.AddChild(DrawBrowse());
            }
            else
            {
                if(previewWindow != null)
                {
                    if(browserContainer != null)
                        browserContainer.Clear();
                    
                    previewWindow.RemoveFromHierarchy();
                    previewWindow = null;
                    previewImage = null;
                }
            }
        }
        void OnEnable()
        {
            instance = UTKScriptableShapeGen.instance;

            if (instance.settings == null)
            {
                instance.settings = new UTKPropSettings();
            }

            prop = instance.settings;

            if (String.IsNullOrEmpty(prop.id))
            {
                prop.id = UnityEngine.Random.Range(int.MinValue, int.MaxValue) + "-utk-" + this.GetHashCode();
            }

            canvas = new VisualElement().Size(100, 100, true, true);
            containers = new();
            rightPanel = new VisualElement().Size(70, 100, true, true);
            leftPanel = new VisualElement().Size(30, 100, true, true).Padding(Utk.ScreenRatio(5)).SetOverflow(Overflow.Hidden);

            rootVisualElement.OnGeometryChanged(x =>
            {
                rootVisualElement.schedule.Execute(() =>
                {
                    if (prop.points.Count == 0)
                        return;

                    for (int i = 0; i < prop.points.Count; i++)
                    {
                        var siz = roots[prop.points[i].root].ElementAt(0).ChangeCoordinatesTo(canvas, roots[prop.points[i].root].ElementAt(0).transform.position);
                        prop.points[i] = (siz, prop.points[i].root);
                    }
                    Draw();
                }).ExecuteLater(5);
            });

            DrawCanvas();
            DrawTab();
        }
        private VisualElement Separator()
        {
            return new VisualElement().Size(100, Utk.ScreenRatio(25), true, false);
        }
        private void DrawTab()
        {
            var tb = new UTKTab(new List<string> { "LineEdit", "SaveAs", "Browse" });
            leftPanel.AddChild(tb);
            
            tb.onTabChanged += x=>
            {
                if(x == 2)
                {
                    DrawPreviewOverlay(true);
                }
                else
                {
                    DrawPreviewOverlay(false);
                }
            };

            var btnCon = new VisualElement().FlexRow().Size(100, Utk.ScreenRatio(50), true, false);
            var btnDrawLines = new Button().Size(20, 100, true, true).Text("D.Line").BcgColor(Color.clear).Name("btnLine");
            var btnFillLines = new Button().Size(20, 100, true, true).Text("Fill").BcgColor(Color.clear).Name("btnFill");
            var btnClear = new Button().Size(20, 100, true, true).Text("Clear").BcgColor(Color.clear);
            btnCon.AddChild(btnDrawLines).AddChild(btnFillLines).AddChild(btnClear);
            tb.InsertToTab(1, btnCon);

            var savAs = new VisualElement().FlexRow().Size(100, Utk.ScreenRatio(50), true, false);
            var txtPath = new TextField().Size(75, 100, true, true);
            txtPath.SetValueWithoutNotify("<EnterName>");
            var btnSave = new Button().Size(20, 100, true, true).Text("Export").BcgColor(Color.clear);
            savAs.AddChild(txtPath).AddChild(btnSave);
            tb.InsertToTab(2, savAs);
            browserContainer = new VisualElement();
            tb.InsertToTab(3, browserContainer);

            btnSave.clicked += () =>
            {
                if (String.IsNullOrEmpty(txtPath.value) || txtPath.value == "<EnterName>")
                    return;

                var vimg = ScriptableObject.CreateInstance<VectorImage>();
                AssetDatabase.CreateAsset(vimg, "Assets/UTKShapeGen/Resources/" + txtPath.value + ".asset");
                vimg.name = txtPath.value;
                prop.utkMesh.meshProperty.painter.SaveToVectorImage(vimg);
                AssetDatabase.SaveAssets();
            };

            if (prop.fill)
                btnFillLines.BcgColor(Color.blue);

            if (prop.line)
                btnDrawLines.BcgColor(Color.blue);

            btnDrawLines.clicked += () =>
            {
                prop.line = !prop.line;

                if (prop.line)
                {
                    btnDrawLines.BcgColor(Color.blue);
                }
                else
                {
                    btnDrawLines.BcgColor(Color.clear);
                }

                ReDraw(5);
            };

            btnFillLines.clicked += () =>
            {
                prop.fill = !prop.fill;

                if (prop.fill)
                {
                    btnFillLines.BcgColor(Color.blue);
                }
                else
                {
                    btnFillLines.BcgColor(Color.clear);
                }

                ReDraw(1);
            };

            btnClear.clicked += () =>
            {
                Clear();
            };

            //////
            var lineCon = new VisualElement().Size(100, Utk.ScreenRatio(50), true, false).FlexRow().AlignItems(Align.FlexStart);
            var fillCon = new VisualElement().Size(100, Utk.ScreenRatio(50), true, false).FlexRow().AlignItems(Align.FlexStart);

            var lblLine = new Label().Text("Line Color").Size(40, 100, true, true).TextAlignment(TextAnchor.MiddleLeft);
            var lblfill = new Label().Text("Fill Color").Size(40, 100, true, true).TextAlignment(TextAnchor.MiddleLeft);
            var lineCol = new ColorField().Size(60, 100, true, true).Name("lineColor");
            var fillCol = new ColorField().Size(60, 100, true, false).Name("fillColor");
            fillCol.value = prop.fillColor;
            lineCol.value = prop.lineColor;

            lineCon.AddChild(lblLine as VisualElement).AddChild(lineCol);
            fillCon.AddChild(lblfill as VisualElement).AddChild(fillCol);
            tb.InsertToTab(1, Separator()).InsertToTab(1, lineCon).InsertToTab(1, fillCon);

            var dcon = new VisualElement().Size(100, Utk.ScreenRatio(50), true, false).FlexRow();
            var lbld = new Label().Text("JoinType").Size(40, 100, true, true).TextAlignment(TextAnchor.MiddleLeft);
            var dropLine = new DropdownField().Size(60, 100, true, true);
            dcon.AddChild(lbld).AddChild(dropLine);
            dropLine.choices = new List<string> { "Mitter", "Round", "Bevel" };
            tb.InsertToTab(1, Separator()).InsertToTab(1, dcon);

            dropLine.value = prop.lineJoin.ToString();

            var lcon = new VisualElement().Size(100, Utk.ScreenRatio(50), true, false).FlexRow();
            var lbll = new Label().Size(40, 100, true, true).Text("LineWidth").TextAlignment(TextAnchor.MiddleLeft);
            var sliderl = new Slider().Size(60, 100, true, true).Name("lineWidth");
            sliderl.LowValue(0).HighValue(100).ShowInputField(true);
            lcon.AddChild(lbll).AddChild(sliderl);
            tb.InsertToTab(1, Separator()).InsertToTab(1, lcon);
            sliderl.value = prop.lineRadius;

            var profCon = new VisualElement().Size(100, Utk.ScreenRatio(50), true, false).FlexRow();
            var btnprof = new Button().Size(20, 100, true, true).Text("Save");
            var lblprof = new Label().Size(40, 100, true, true).Text("SavePreset").TextAlignment(TextAnchor.MiddleLeft);
            var prof = new TextField().Size(35, 100, true, true);
            prof.SetValueWithoutNotify("<NO_NAME>");
            profCon.AddChild(lblprof).AddChild(prof).AddChild(btnprof);
            tb.InsertToTab(1, Separator().MarginBottom(5)).InsertToTab(1, profCon);

            var profListCon = new VisualElement().Size(100, Utk.ScreenRatio(50), true, false).FlexRow();
            var lblprofList = new Label().Size(40, 100, true, true).Text("LoadPreset").TextAlignment(TextAnchor.MiddleLeft);
            var profList = new DropdownField().Size(60, 100, true, true);
            profListCon.AddChild(lblprofList).AddChild(profList);
            tb.InsertToTab(1, profListCon);

            List<string> lis = new();

            if (instance.profiles.Count > 0)
            {
                foreach (var str in instance.profiles)
                {
                    lis.Add(str.name);
                }
            }

            lis.Add("<None>");
            profList.choices = lis;
            profList.SetValueWithoutNotify("<SelectProfile>");

            profList.OnValueChanged(x =>
            {
                if (String.IsNullOrEmpty(x.newValue))
                    return;

                if (instance.profiles.Exists(y => y.name == x.newValue))
                {
                    Clear();

                    var found = instance.profiles.Find(z => z.name == x.newValue);

                    var newprop = new UTKPropSettings();
                    newprop.name = prof.value;
                    newprop.fill = found.fill;
                    newprop.fillColor = found.fillColor;
                    newprop.line = found.line;
                    newprop.lineCap = found.lineCap;
                    newprop.lineColor = found.lineColor;
                    newprop.lineJoin = found.lineJoin;
                    newprop.lineRadius = found.lineRadius;
                    newprop.points = new List<(Vector3 point, int root)>(found.points);

                    prop = newprop;

                    rootVisualElement.schedule.Execute(() =>
                    {
                        var colline = rootVisualElement.Query<ColorField>("lineColor").First();
                        colline.SetValueWithoutNotify(prop.lineColor);

                        var colfill = rootVisualElement.Query<ColorField>("fillColor").First();
                        colfill.SetValueWithoutNotify(prop.fillColor);

                        var bfill = rootVisualElement.Query<Button>("btnFill").First();
                        var bline = rootVisualElement.Query<Button>("btnLine").First();

                        if (prop.fill)
                            bfill.BcgColor(Color.blue);
                        else
                            bfill.BcgColor(Color.clear);

                        if (prop.line)
                            bline.BcgColor(Color.blue);
                        else
                            bline.BcgColor(Color.clear);

                        Draw();
                    }).ExecuteLater(5);
                }


            });

            btnprof.clicked += () =>
            {
                if (String.IsNullOrEmpty(prof.value) || prof.value == "<NO_NAME>" || instance.profiles.Exists(x => x.name == prof.value))
                {
                    Debug.LogWarning("UTKShapeGen : Profile name can't be empty or it already existed. Aborting!");
                    return;
                }
                var newprop = new UTKPropSettings();
                newprop.name = prof.value;
                newprop.fill = prop.fill;
                newprop.fillColor = prop.fillColor;
                newprop.line = prop.line;
                newprop.lineCap = prop.lineCap;
                newprop.lineColor = prop.lineColor;
                newprop.lineJoin = prop.lineJoin;
                newprop.lineRadius = prop.lineRadius;
                newprop.points = new List<(Vector3 point, int root)>(prop.points);


                instance.profiles.Add(newprop);

                List<string> lis = new();

                if (instance.profiles.Count > 0)
                {
                    foreach (var str in instance.profiles)
                    {
                        lis.Add(str.name);
                    }
                }

                lis.Add("<None>");
                profList.choices = lis;
                profList.SetValueWithoutNotify(profList.value);

                prof.value = string.Empty;
                EditorUtility.SetDirty(instance);
                instance.SaveThis();
            };

            sliderl.OnValueChanged(x =>
            {
                prop.lineRadius = x.newValue;
                ReDraw(5);
            });

            dropLine.OnValueChanged(x =>
            {
                if (x.newValue == "Mitter")
                {
                    prop.lineJoin = LineJoin.Miter;
                }
                else if (x.newValue == "Round")
                {
                    prop.lineJoin = LineJoin.Round;
                }
                else
                {
                    prop.lineJoin = LineJoin.Bevel;
                }

                Draw();
            });

            lineCol.OnValueChanged(x =>
            {
                prop.lineColor = x.newValue;
                ReDraw(5);
            });

            fillCol.OnValueChanged(x =>
            {
                prop.fillColor = x.newValue;
                ReDraw(5);
            });
        }
        private void Clear()
        {
            if (prop.utkMesh != null)
                prop.utkMesh.RemoveFromHierarchy();

            if (prop.points.Count > 0)
            {
                for (int i = prop.points.Count; i-- > 0;)
                {
                    roots[prop.points[i].root].BcgColor(Color.clear);
                }

                prop.points.Clear();
            }
        }
        private void DrawCanvas()
        {
            this.rootVisualElement.AddChild(leftPanel).AddChild(rightPanel).FlexRow();
            rightPanel.AddChild(canvas);

            rootVisualElement.schedule.Execute(() =>
            {
                DrawGrid(60, 50);
            }).ExecuteLater(1);
        }
        private void DrawGrid(int row, int column)
        {
            float width = 100 / (float)row;
            float height = 100 / (float)column;

            for (int i = 0; i < column; i++)
            {
                var vis = new VisualElement().FlexRow().AlignItems(Align.FlexStart).Size(100, height, true, true).Name(i.ToString());
                canvas.AddChild(vis);
                containers.Add(vis);
            }


            for (int i = 0; i < row * column; i++)
            {
                for (int j = 0; j < containers.Count; j++)
                {
                    if (containers[j].childCount < row)
                    {
                        var root = new VisualElement().Size(100, 100, true, true).Border(Utk.ScreenRatio(1f), Color.white).Opacity(0.3f);
                        var sub = new VisualElement().Size(50, 50, true, true).BcgColor(Color.clear).Name("point").Position(Position.Absolute).Bottom(0).Right(0);

                        root.OnMouseDown(x =>
                        {
                            if (!roots.Exists(x => x == root))
                            {
                                root.BcgColor(Color.yellow);

                                var siz = sub.ChangeCoordinatesTo(canvas, sub.transform.position);
                                roots.Add(root);
                                prop.points.Add((siz, roots.Count - 1));
                                canvas.schedule.Execute(() =>
                                {
                                    Draw();
                                }).ExecuteLater(5);
                            }
                            else
                            {
                                root.BcgColor(Color.clear);

                                for (int u = 0; u < prop.points.Count; u++)
                                {
                                    if (roots[prop.points[u].root] == root)
                                    {
                                        prop.points.RemoveAt(u);

                                        canvas.schedule.Execute(() =>
                                        {
                                            Draw();
                                        }).ExecuteLater(5);
                                        break;
                                    }
                                }
                            }
                        });

                        root.AddChild(sub);
                        containers[j].AddChild(root);
                        break;
                    }
                }
            }
        }

        private void Draw()
        {
            if (this.prop.points.Count == 0)
                return;

            List<Vector3> p = new();
            UTKMeshProp prop = new UTKMeshProp();

            foreach (var f in this.prop.points)
            {
                p.Add(f.point);
            }

            if (this.prop.utkMesh != null)
                this.prop.utkMesh.RemoveFromHierarchy();

            prop.lineJoin = this.prop.lineJoin;
            prop.lineCap = this.prop.lineCap;

            if (this.prop.line)
            {
                prop.lineColor = this.prop.lineColor;
                prop.lineRadius = this.prop.lineRadius;
            }
            else
            {
                prop.lineRadius = 0f;
            }

            if (this.prop.fill)
                prop.fillColor = this.prop.fillColor;

            this.prop.utkMesh = new UTKMesh(p, prop);
            this.prop.utkMesh.Position(Position.Absolute).Opacity(0.7f);
            canvas.AddChild(this.prop.utkMesh);

        }

        private void ReDraw(long waitTime)
        {
            canvas.schedule.Execute(() =>
            {
                Draw();
            }).ExecuteLater(waitTime);
        }
        private VisualElement DrawBrowse()
        {
            var vis = new VisualElement().FlexGrow(1).Size(100, 90, true, true);
            var assets = FindAssetsByType();

            Func<VisualElement> makeItem = ()=>
            {
                var vis = new VisualElement().FlexRow().Size(100, Utk.ScreenRatio(50), true, false);
                var lbl = new Label().Size(80, 100, true, true).TextAlignment(TextAnchor.MiddleLeft).MarginLeft(5);
                var img = new Image().Size(20, 100, true, true);
                vis.AddChild(lbl).AddChild(img);
                return vis;
            };

            Action<VisualElement, int> bindItem = (vis, i)=>
            {
                (vis.ElementAt(0) as Label).Text(assets[i].obj.name);
                (vis.ElementAt(1) as Image).image = assets[i].thumbnail;
                (vis.ElementAt(1) as Image).scaleMode = ScaleMode.ScaleToFit;

                void Down(MouseDownEvent evt)
                {
                    previewImage.vectorImage= assets[i].obj;
                    previewImage.MarkDirtyRepaint();                    
                }
                vis.userData = new Action(()=> vis.UnregisterCallback<MouseDownEvent>(Down));
                vis.RegisterCallback<MouseDownEvent>(Down);
            };

            Action<VisualElement, int> unbindItem = (vis, i)=>
            {
                var act = vis.userData as Action;
                act?.Invoke();                
            };

            const int itemHeight = 30;
            var listview = new ListView(assets, itemHeight, makeItem, bindItem);
            listview.unbindItem = unbindItem;
            vis.Add(listview);
            return vis;
        }
        private List<(VectorImage obj, Texture2D thumbnail)> FindAssetsByType()
        {
            string[] guids;
            List<(VectorImage, Texture2D)> lis = new();
            guids = AssetDatabase.FindAssets("t:VectorImage", new[] {"Assets/UTKShapeGen/Resources"});

            if (guids == null || guids.Length == 0)
                return lis;

            foreach (string guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath(path, typeof(VectorImage)) as VectorImage;
                var prev = AssetPreview.GetAssetPreview(obj);
                lis.Add((obj, prev));
            }

            return lis;
        }
    }
}
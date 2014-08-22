﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Foreman
{
	public enum LinkType {Input, Output};

	public partial class NodeElement : GraphElement
	{
		public int DragOffsetX;
		public int DragOffsetY;

		public bool Moused { get { return Parent.MousedNode == this; }}
		public Point MousePosition = Point.Empty;

		private Color recipeColour = Color.FromArgb(190, 217, 212);
		private Color supplyColour = Color.FromArgb(249, 237, 195);
		private Color consumerColour = Color.FromArgb(231, 214, 224);
		private Color backgroundColour;

		public const int iconSize = 32;
		public const int iconBorder = 4;
		public const int iconTextHeight = 10;

		private string rateText = "";
		private string nameText = "";

		TextBox editorBox;
		Item editedItem;
		LinkType editedLinkType;
		float originalEditorValue;
		
		public ProductionNode DisplayedNode { get; private set; }

		public NodeElement(ProductionNode node, ProductionGraphViewer parent) : base(parent)
		{
			Width = 100;
			Height = 80;

			DisplayedNode = node;

			if (DisplayedNode.GetType() == typeof(ConsumerNode))
			{
				backgroundColour = supplyColour;
			}
			else if (DisplayedNode.GetType() == typeof(SupplyNode))
			{
				backgroundColour = consumerColour;
			}
			else
			{
				backgroundColour = recipeColour;
			}
		}
		
		public void Update()
		{
			if (DisplayedNode.GetType() == typeof(RecipeNode))
			{
				nameText = String.Format("Recipe: {0}", DisplayedNode.DisplayName);
			}
			else if (DisplayedNode.GetType() == typeof(ConsumerNode))
			{
				nameText = String.Format("Output: {0}", DisplayedNode.DisplayName);
			}
			else if (DisplayedNode.GetType() == typeof(SupplyNode))
			{
				nameText = String.Format("Input: {0}", DisplayedNode.DisplayName);
			}

			SizeF stringSize = Parent.CreateGraphics().MeasureString(nameText, SystemFonts.DefaultFont);
			Width = Math.Max((int)stringSize.Width + iconBorder * 2, getIconWidths());
		}

		private int getIconWidths()
		{
			return Math.Max(
				(iconSize + iconBorder * 5) * DisplayedNode.Outputs.Count() + iconBorder,
				(iconSize + iconBorder * 5) * DisplayedNode.Inputs.Count() + iconBorder);
		}

		public Rectangle GetIconBounds(Item item, LinkType linkType)
		{
			int X = 0;
			int Y = 0;
			int	Width = iconSize + iconBorder + iconBorder;
			int	Height = iconSize + iconBorder + iconBorder + iconTextHeight;

			if (linkType == LinkType.Output)
			{
				Point iconPoint = GetOutputIconPoint(item);
				var sortedOutputs = DisplayedNode.Outputs.OrderBy(i => getXSortValue(i, LinkType.Output)).ToList();
				X = iconPoint.X - Width / 2;
				Y = iconPoint.Y -(iconSize + iconBorder + iconBorder + iconTextHeight) / 2;
			}
			else
			{
				Point iconPoint = GetInputIconPoint(item);
				var sortedInputs = DisplayedNode.Inputs.OrderBy(i => getXSortValue(i, LinkType.Input)).ToList();
				X = iconPoint.X - Width / 2;
				Y = iconPoint.Y -(iconSize + iconBorder + iconBorder) / 2;
			}

			return new Rectangle(X, Y, Width, Height);
		}

		public Point GetOutputIconPoint(Item item)
		{
			if (DisplayedNode.Outputs.Contains(item))
			{
				var sortedOutputs = DisplayedNode.Outputs.OrderBy(i => getXSortValue(i, LinkType.Output)).ToList();
				int x = Convert.ToInt32((float)Width / (sortedOutputs.Count()) * (sortedOutputs.IndexOf(item) + 0.5f));
				int y = 0;
				return new Point(x, y + iconBorder);
			}
			else
			{
				return new Point();
			}
		}

		public Point GetInputIconPoint(Item item)
		{
			if (DisplayedNode.Inputs.Contains(item))
			{
				var sortedInputs = DisplayedNode.Inputs.OrderBy(i => getXSortValue(i, LinkType.Input)).ToList();
				int x = Convert.ToInt32((float)Width / (sortedInputs.Count()) * (sortedInputs.IndexOf(item) + 0.5f));
				int y = Height;
				return new Point(x, y - iconBorder);
			}
			else
			{
				return new Point();
			}
		}

		public Point GetOutputLineConnectionPoint(Item item)
		{
			return Point.Add(GetOutputIconPoint(item), new Size(X, Y - (iconSize + iconBorder + iconBorder) / 2 - iconTextHeight));
		}

		public Point GetInputLineConnectionPoint(Item item)
		{
			return Point.Add(GetInputIconPoint(item), new Size(X, Y + (iconSize + iconBorder + iconBorder) / 2 + iconTextHeight));
		}

		//Used to sort items in the input/output lists
		public int getXSortValue(Item item, LinkType linkType)
		{
			int total = 0;
			if (linkType == LinkType.Input)
			{
				foreach (NodeLink link in DisplayedNode.InputLinks.Where(l => l.Item == item))
				{
					total += Parent.GetElementForNode(link.Supplier).X;
				}
			}
			else
			{
				foreach (NodeLink link in DisplayedNode.OutputLinks.Where(l => l.Item == item))
				{
					total += Parent.GetElementForNode(link.Consumer).X;
				}
			}
			return total;
		}

		public override void Paint(Graphics graphics)
		{
			using (SolidBrush brush = new SolidBrush(backgroundColour))
			{
				GraphicsStuff.FillRoundRect(0, 0, Width, Height, 8, graphics, brush);
			}

			if (Parent.ClickedNode == this)
			{
				using (Pen pen = new Pen(Color.WhiteSmoke, 3f))
				{
					GraphicsStuff.DrawRoundRect(0, 0, Width, Height, 8, graphics, pen);
				}
			}
			else if (Parent.MousedNode == this)
			{
				using (Pen pen = new Pen(Color.LightGray, 3f))
				{
					GraphicsStuff.DrawRoundRect(0, 0, Width, Height, 8, graphics, pen);
				}
			}
			else if (Parent.SelectedNode == this)
			{
				using (Pen pen = new Pen(Color.DarkGray, 3f))
				{
					GraphicsStuff.DrawRoundRect(0, 0, Width, Height, 8, graphics, pen);
				}
			}

			String unit = "";
			if (Parent.Graph.SelectedAmountType == AmountType.Rate && Parent.Graph.SelectedUnit == RateUnit.PerSecond)
			{
				unit = "/s";
			}
			else if (Parent.Graph.SelectedAmountType == AmountType.Rate && Parent.Graph.SelectedUnit == RateUnit.PerMinute)
			{
				unit = "/m";
			}
			String formatString = "{0:0.##}{1}";
			foreach (Item item in DisplayedNode.Outputs)
			{
				DrawItemIcon(item, GetOutputIconPoint(item), LinkType.Output, String.Format(formatString, DisplayedNode.GetTotalOutput(item), unit), graphics);
			}
			foreach (Item item in DisplayedNode.Inputs)
			{
				DrawItemIcon(item, GetInputIconPoint(item), LinkType.Input, String.Format(formatString, DisplayedNode.GetTotalInput(item), unit), graphics);
			}

			StringFormat centreFormat = new StringFormat();
			centreFormat.Alignment = centreFormat.LineAlignment = StringAlignment.Center;
			graphics.DrawString(nameText, new Font(FontFamily.GenericSansSerif, 8), new SolidBrush(Color.Black), new PointF(Width / 2, Height / 2), centreFormat);

			if (editorBox != null)
			{
				TooltipInfo ttinfo = new TooltipInfo();
				ttinfo.ScreenLocation = Parent.GraphToScreen(GetInputLineConnectionPoint(editedItem));
				ttinfo.Direction = Direction.Up;
				ttinfo.ScreenSize = new Point(editorBox.Size);
				Parent.AddTooltip(ttinfo);
			}
		}

		private void DrawItemIcon(Item item, Point drawPoint, LinkType linkType, String rateText, Graphics graphics)
		{
			int boxSize = iconSize + iconBorder + iconBorder;
			StringFormat centreFormat = new StringFormat();
			centreFormat.Alignment = centreFormat.LineAlignment = StringAlignment.Center;

			using (Pen borderPen = new Pen(Color.Gray, 3))
			using (Brush fillBrush = new SolidBrush(Color.White))
			using (Brush textBrush = new SolidBrush(Color.Black))
			{
				if (linkType == LinkType.Output)
				{
					GraphicsStuff.FillRoundRect(drawPoint.X - (boxSize / 2), drawPoint.Y - (boxSize / 2) - iconTextHeight, boxSize, boxSize + iconTextHeight, iconBorder, graphics, fillBrush);
					GraphicsStuff.DrawRoundRect(drawPoint.X - (boxSize / 2), drawPoint.Y - (boxSize / 2) - iconTextHeight, boxSize, boxSize + iconTextHeight, iconBorder, graphics, borderPen);
					graphics.DrawString(rateText, new Font(FontFamily.GenericSansSerif, iconTextHeight - iconBorder + 1), textBrush, new PointF(drawPoint.X, drawPoint.Y - (boxSize + iconTextHeight) / 2 + iconBorder), centreFormat);
				}
				else
				{
					GraphicsStuff.FillRoundRect(drawPoint.X - (boxSize / 2), drawPoint.Y - (boxSize / 2), boxSize, boxSize + iconTextHeight, iconBorder, graphics, fillBrush);
					GraphicsStuff.DrawRoundRect(drawPoint.X - (boxSize / 2), drawPoint.Y - (boxSize / 2), boxSize, boxSize + iconTextHeight, iconBorder, graphics, borderPen);
					graphics.DrawString(rateText, new Font(FontFamily.GenericSansSerif, 7), textBrush, new PointF(drawPoint.X, drawPoint.Y + (boxSize + iconTextHeight) / 2 - iconBorder), centreFormat);
				}
			}
			graphics.DrawImage(item.Icon ?? DataCache.UnknownIcon, drawPoint.X - iconSize / 2, drawPoint.Y - iconSize / 2, iconSize, iconSize);
		}

		public override void MouseUp(Point location, MouseButtons button)
		{
			Item clickedItem = null;
			LinkType clickedLinkType = LinkType.Input;
			foreach (Item item in DisplayedNode.Inputs)
			{
				if (GetIconBounds(item, LinkType.Input).Contains(location))
				{
					clickedItem = item;
					clickedLinkType = LinkType.Input;
				}
			}
			foreach (Item item in DisplayedNode.Outputs)
			{
				if (GetIconBounds(item, LinkType.Output).Contains(location))
				{
					clickedItem = item;
					clickedLinkType = LinkType.Output;
				}
			}

			if (button == MouseButtons.Left)
			{
				if (clickedItem != null && clickedLinkType == LinkType.Input && DisplayedNode is ConsumerNode)
				{
					BeginEditingInputAmount(clickedItem);
				}
			}
			else if (button == MouseButtons.Right)
			{
				ContextMenu rightClickMenu = new ContextMenu();
				if (clickedItem != null)
				{
					if (clickedLinkType == LinkType.Input)
					{
						if (DisplayedNode.GetExcessDemand(clickedItem) > 0)
						{
							rightClickMenu.MenuItems.Add(new MenuItem("Automatically choose/create a node to produce this item",
								new EventHandler((o, e) =>
								{
									DisplayedNode.Graph.AutoSatisfyNodeDemand(DisplayedNode, clickedItem);
									Parent.UpdateElements();
									Parent.Invalidate();
								})));

							rightClickMenu.MenuItems.Add(new MenuItem("Manually create a node to produce this item",
								new EventHandler((o, e) =>
									{
										RecipeChooserForm form = new RecipeChooserForm(clickedItem);
										var result = form.ShowDialog();
										if (result == DialogResult.OK)
										{
											if (form.selectedRecipe != null)
											{
												DisplayedNode.Graph.CreateRecipeNodeToSatisfyItemDemand(DisplayedNode, clickedItem, form.selectedRecipe);
											}
											else
											{
												DisplayedNode.Graph.CreateSupplyNodeToSatisfyItemDemand(DisplayedNode, clickedItem);
											}
											Parent.UpdateElements();
											Parent.Invalidate();
										}
									})));

							rightClickMenu.MenuItems.Add(new MenuItem("Connect this input to an existing node",
								new EventHandler((o, e) =>
								{
									DraggedLinkElement newLink = new DraggedLinkElement(Parent, this, LinkType.Input, clickedItem);
									newLink.ConsumerElement = this;
									})));
						}
					}
					else
					{
						rightClickMenu.MenuItems.Add(new MenuItem("Connect this output to an existing node",
							new EventHandler((o, e) =>
								{
									DraggedLinkElement newLink = new DraggedLinkElement(Parent, this, LinkType.Output, clickedItem);
									newLink.SupplierElement = this;
								})));
					}
				}

				rightClickMenu.MenuItems.Add(new MenuItem("Delete node",
					new EventHandler((o, e) =>
						{
							Parent.DeleteNode(this);
						})));

				rightClickMenu.Show(Parent, Parent.GraphToScreen(Point.Add(location, new Size(X, Y))));
			}
		}

		public void BeginEditingInputAmount(Item item)
		{
			if (editorBox != null)
			{
				StopEditingInputAmount();
			}

			editorBox = new TextBox();
			editedItem = item;
			editedLinkType = LinkType.Input;
			editorBox.Text = (DisplayedNode as ConsumerNode).ConsumptionAmount.ToString();
			originalEditorValue = (DisplayedNode as ConsumerNode).ConsumptionAmount;
			editorBox.SelectAll();
			editorBox.Size = new Size(100, 30);
			Rectangle tooltipRect = Parent.getTooltipScreenBounds(Parent.GraphToScreen(GetInputLineConnectionPoint(item)), new Point(editorBox.Size), Direction.Up);
			editorBox.Location = new Point(tooltipRect.X, tooltipRect.Y);
			Parent.Controls.Add(editorBox);
			editorBox.Focus();
			editorBox.TextChanged += editorBoxTextChanged;
			editorBox.KeyDown += new KeyEventHandler(editorBoxKeyDown);
		}

		public void StopEditingInputAmount()
		{
			Parent.Controls.Remove(editorBox);
			editorBox.Dispose();
			editorBox = null;
			editedItem = null;
			Parent.Focus();
		}

		public void GraphViewMoved()
		{
			if (editorBox != null)
			{
				StopEditingInputAmount();
			}
		}

		public void editorBoxTextChanged(object sender, EventArgs e)
		{
			float amount;
			bool amountIsValid = float.TryParse((sender as TextBox).Text, out amount);

			if (amountIsValid)
			{
				(DisplayedNode as ConsumerNode).ConsumptionAmount = amount;
				DisplayedNode.Graph.UpdateNodeAmounts();
				Parent.Invalidate();
			}
		}

		public void editorBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				StopEditingInputAmount();
			}
			else if (e.KeyCode == Keys.Escape)
			{
				editorBox.Text = originalEditorValue.ToString();
				StopEditingInputAmount();
			}			
		}

		public override void MouseMoved(Point location)
		{
			if (editorBox == null)
			{
				foreach (Item item in DisplayedNode.Inputs)
				{
					if (GetIconBounds(item, LinkType.Input).Contains(location))
					{
						String tooltipText;
						if (DisplayedNode is ConsumerNode)
						{
							tooltipText = String.Format("{0} (Click to edit amount)", item.Name);
						}
						else
						{
							tooltipText = item.Name;
						}

						Parent.AddTooltip(new TooltipInfo(Parent.GraphToScreen(GetInputLineConnectionPoint(item)), new Point(), Direction.Up, tooltipText));
					}
				}
				foreach (Item item in DisplayedNode.Outputs)
				{
					if (GetIconBounds(item, LinkType.Output).Contains(location))
					{
						Parent.AddTooltip(new TooltipInfo(Parent.GraphToScreen(GetOutputLineConnectionPoint(item)), new Point(), Direction.Down, item.Name));
					}
				}
			}
		}

		public override bool ContainsPoint(Point point)
		{
			if (new Rectangle(0, 0, Width, Height).Contains(point.X, point.Y))
			{
				return true;
			}
			foreach (Item item in DisplayedNode.Inputs)
			{
				Rectangle iconBounds = GetIconBounds(item, LinkType.Input);
				if (iconBounds.Contains(point.X, point.Y))
				{
					return true;
				}
			}
			foreach (Item item in DisplayedNode.Outputs)
			{
				Rectangle iconBounds = GetIconBounds(item, LinkType.Output);
				iconBounds.Offset(X, Y);
				if (iconBounds.Contains(point.X, point.Y))
				{
					return true;
				}
			}
			return false;
		}

		public override void MouseDown(Point location, MouseButtons button)
		{
			if (button == MouseButtons.Left)
			{
				Parent.DraggedElement = this;
				DragOffsetX = location.X;
				DragOffsetY = location.Y;
			}
		}

		public override void Dragged(Point location)
		{
			X += location.X - DragOffsetX;
			Y += location.Y - DragOffsetY;
		}
	}
}
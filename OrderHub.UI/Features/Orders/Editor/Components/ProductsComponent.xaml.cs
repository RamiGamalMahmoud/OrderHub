using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OrderHub.UI.Features.Orders.Editor.Components
{
    public partial class ProductsComponent : UserControl
    {
        public ProductsComponent()
        {
            InitializeComponent();
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DependencyObject focusedVisual = Keyboard.FocusedElement as DependencyObject;
                if (focusedVisual == null) return;

                // Check if the Shift key is held down
                bool isShiftPressed = Keyboard.Modifiers == ModifierKeys.Shift;

                if (isShiftPressed)
                {
                    // --- BACKWARD NAVIGATION (Shift + Enter) ---

                    // 1. From Add Button -> Go to Quantity Input
                    if (focusedVisual == AddProductButton)
                    {
                        e.Handled = true;
                        FocusNumericUpDownInnerTextBox(QuantityInput);
                        return;
                    }

                    // 2. From Quantity Input -> Go to Price Input
                    if (IsVisualDescendantOf(focusedVisual, QuantityInput))
                    {
                        e.Handled = true;
                        FocusNumericUpDownInnerTextBox(PriceInput);
                        return;
                    }

                    // 3. From Price Input -> Go to ListView
                    if (IsVisualDescendantOf(focusedVisual, PriceInput))
                    {
                        e.Handled = true;
                        ProductListView.Focus();
                        return;
                    }

                    // 4. From ListView -> Go to SearchBar
                    if (IsVisualDescendantOf(focusedVisual, ProductListView) || focusedVisual is ListViewItem)
                    {
                        e.Handled = true;
                        ProductSearchBar.Focus();
                        ProductSearchBar.SelectAll();
                        return;
                    }
                }
                else
                {
                    // --- FORWARD NAVIGATION (Enter Only) ---

                    // 1. From SearchBar -> Go to ListView
                    if (IsVisualDescendantOf(focusedVisual, ProductSearchBar))
                    {
                        e.Handled = true;
                        ProductListView.Focus();
                        if (ProductListView.Items.Count > 0 && ProductListView.SelectedIndex == -1)
                        {
                            ProductListView.SelectedIndex = 0;
                        }
                        return;
                    }

                    // 2. From ListView -> Go to Price Input
                    if (IsVisualDescendantOf(focusedVisual, ProductListView) || focusedVisual is ListViewItem)
                    {
                        e.Handled = true;
                        FocusNumericUpDownInnerTextBox(PriceInput);
                        return;
                    }

                    // 3. From Price Input -> Go to Quantity Input
                    if (IsVisualDescendantOf(focusedVisual, PriceInput))
                    {
                        e.Handled = true;
                        FocusNumericUpDownInnerTextBox(QuantityInput);
                        return;
                    }

                    // 4. From Quantity Input -> Go to Add Button
                    if (IsVisualDescendantOf(focusedVisual, QuantityInput))
                    {
                        e.Handled = true;
                        AddProductButton.Focus();
                        return;
                    }

                    // 5. From Add Button -> Let Enter click it natively
                    if (focusedVisual == AddProductButton)
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Finds the encapsulated text control inside HandyControl's NumericUpDown wrapper
        /// and applies direct keyboard focus to allow rapid number entry.
        /// </summary>
        private void FocusNumericUpDownInnerTextBox(Control numericUpDown)
        {
            if (numericUpDown == null) return;

            if (numericUpDown.Template.FindName("PART_TextBox", numericUpDown) is TextBox innerTextBox)
            {
                innerTextBox.Focus();
                innerTextBox.SelectAll(); // Selects content for instant overwrite
            }
            else
            {
                numericUpDown.Focus();
            }
        }

        /// <summary>
        /// Fires when the item gets submitted to the collection. Loops focus back to search.
        /// </summary>
        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            ProductSearchBar.Focus();
        }

        /// <summary>
        /// Visual helper tree scanning engine.
        /// </summary>
        private bool IsVisualDescendantOf(DependencyObject child, DependencyObject parent)
        {
            if (child == parent) return true;

            DependencyObject current = child;
            while (current != null)
            {
                if (current == parent) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }
    }
}

// OrderService.cs — Order Business Logic Service
//
// Contains the core POS workflow: creating orders, calculating totals,
// managing order status transitions, and coordinating with table management.
// Connects to: OrderDAL.cs (database), TableDAL.cs (table status updates),
//              InventoryService.cs (stock deduction on completion),
//              OrderForm.cs (UI), BillingForm.cs (billing UI)

using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;

namespace CoffeeShopPOS.Services
{
    /// <summary>
    /// Business logic for the order workflow — the heart of the POS system.
    /// Manages the complete order lifecycle from placement to completion.
    /// </summary>
    public static class OrderService
    {
        // Tax rate used for all orders (10%)
        // This could be moved to a settings table in the database for configurability
        public const decimal TaxRate = 0.10m;

        /// <summary>
        /// Places a complete order: creates the order, adds line items, updates table status.
        /// Called by: OrderForm.cs "Place Order" button click handler.
        /// 
        /// Workflow:
        /// 1. Calculate financial totals (subtotal, tax, total)
        /// 2. Insert order record via OrderDAL.Create()
        /// 3. Insert each line item via OrderDAL.AddItem()
        /// 4. If dine-in, set table status to "Occupied" via TableDAL.UpdateStatus()
        /// </summary>
        /// <param name="order">Order with Items collection populated from OrderForm</param>
        /// <returns>The auto-generated order ID from the database</returns>
        public static int PlaceOrder(Order order)
        {
            // Validate the order before processing
            if (order.Items == null || order.Items.Count == 0)
                throw new InvalidOperationException("Cannot place an empty order. Add at least one item.");

            if (order.UserId <= 0)
                throw new InvalidOperationException("No staff member assigned to this order.");

            // Step 1: Calculate totals
            CalculateTotals(order);

            // Step 2: Create the order record in the database
            // OrderDAL.Create() inserts into the 'orders' table and returns the new order_id
            int orderId = OrderDAL.Create(order);
            order.OrderId = orderId;

            // Step 3: Add each line item to the order
            foreach (var item in order.Items)
            {
                item.OrderId = orderId;  // Link item to the newly created order
                OrderDAL.AddItem(item);   // Insert into 'order_items' table
            }

            // Step 4: If this is a dine-in order, mark the table as Occupied
            // This connects to TableDAL.cs which updates the 'tables' table
            if (order.TableId.HasValue && order.OrderType == "Dine-In")
            {
                TableDAL.UpdateStatus(order.TableId.Value, "Occupied");
            }

            return orderId;
        }

        /// <summary>
        /// Calculates subtotal, tax, and total for an order.
        /// Called by: PlaceOrder() above, and BillingForm.cs when applying discounts.
        /// 
        /// Formula:
        ///   Subtotal = Σ (item.Quantity × item.UnitPrice)
        ///   Tax = Subtotal × TaxRate (10%)
        ///   Total = Subtotal + Tax - Discount
        /// </summary>
        public static void CalculateTotals(Order order)
        {
            // Sum up all line item totals (quantity × unit price)
            order.Subtotal = order.Items.Sum(i => i.Quantity * i.UnitPrice);

            // Calculate tax on the subtotal
            order.Tax = Math.Round(order.Subtotal * TaxRate, 2);

            // Final total after tax and discount
            order.Total = order.Subtotal + order.Tax - order.Discount;

            // Ensure total never goes negative
            if (order.Total < 0) order.Total = 0;
        }

        /// <summary>
        /// Updates the status of an order and handles side effects.
        /// Called by: OrderForm.cs and BillingForm.cs status buttons.
        /// 
        /// Status transitions: Pending → Preparing → Served → Completed
        /// When completing: releases the table and triggers inventory deduction.
        /// </summary>
        /// <param name="orderId">ID of the order to update</param>
        /// <param name="newStatus">New status value</param>
        public static void UpdateStatus(int orderId, string newStatus)
        {
            // Validate status value
            var validStatuses = new[] { "Pending", "Preparing", "Served", "Completed", "Cancelled" };
            if (!validStatuses.Contains(newStatus))
                throw new ArgumentException($"Invalid order status: {newStatus}");

            // Get the current order to check its state
            var order = OrderDAL.GetById(orderId);
            if (order == null)
                throw new InvalidOperationException($"Order #{orderId} not found.");

            // Prevent modifying already completed/cancelled orders
            if (order.Status == "Completed" || order.Status == "Cancelled")
                throw new InvalidOperationException(
                    $"Cannot change status of a {order.Status.ToLower()} order.");

            // Update the status in the database via OrderDAL.cs
            OrderDAL.UpdateStatus(orderId, newStatus);

            // Handle side effects based on the new status
            if (newStatus == "Completed")
            {
                // Release the table back to "Available" status
                // This connects to TableDAL.cs to update the tables table
                if (order.TableId.HasValue)
                {
                    TableDAL.UpdateStatus(order.TableId.Value, "Available");
                }

                // Trigger inventory deduction (future enhancement)
                // InventoryService.DeductForOrder(orderId) could be called here
            }
            else if (newStatus == "Cancelled")
            {
                // Release the table if the order is cancelled
                if (order.TableId.HasValue)
                {
                    // Check if there are other active orders for this table
                    var tableOrders = OrderDAL.GetByTable(order.TableId.Value);
                    // Only release if this was the only active order
                    if (tableOrders.Count <= 1)
                    {
                        TableDAL.UpdateStatus(order.TableId.Value, "Available");
                    }
                }
            }
        }

        /// <summary>
        /// Applies a discount via BillingForm.cs and recalculates the total.
        /// </summary>
        public static void ApplyDiscount(int orderId, decimal discountAmount)
        {
            var order = OrderDAL.GetById(orderId);
            if (order == null)
                throw new InvalidOperationException($"Order #{orderId} not found.");

            if (discountAmount < 0)
                throw new ArgumentException("Discount cannot be negative.");

            if (discountAmount > order.Subtotal + order.Tax)
                throw new ArgumentException("Discount cannot exceed the order total.");

            order.Discount = discountAmount;
            CalculateTotals(order);

            // Save updated totals back to database via OrderDAL.cs
            OrderDAL.UpdateTotals(orderId, order.Subtotal, order.Tax, order.Discount, order.Total);
        }

        /// <summary>
        /// Completes payment for an order: sets payment method and marks as completed.
        /// Called by: BillingForm.cs "Complete Payment" button.
        /// This is the final step in the order workflow.
        /// </summary>
        public static void CompletePayment(int orderId, string paymentMethod, decimal discount = 0)
        {
            var order = OrderDAL.GetById(orderId);
            if (order == null)
                throw new InvalidOperationException($"Order #{orderId} not found.");

            // Apply discount if provided
            if (discount > 0)
            {
                ApplyDiscount(orderId, discount);
            }

            // Set payment method
            OrderDAL.UpdatePaymentMethod(orderId, paymentMethod);

            // Mark order as Completed (this also sets completed_at timestamp)
            UpdateStatus(orderId, "Completed");
        }

        /// <summary>
        /// Gets all active (not completed/cancelled) orders.
        /// Called by: OrderForm.cs to show pending orders list.
        /// </summary>
        public static List<Order> GetActiveOrders()
        {
            return OrderDAL.GetActiveOrders();
        }
    }
}

// Table.cs — Restaurant Table Model
//
// Represents a physical table in the restaurant with capacity and status.
// Maps directly to the 'tables' table in MySQL.
// Used by: TableDAL.cs (data access), TableManagementForm.cs (visual grid),
//          OrderForm.cs (table selection for dine-in orders),
//          OrderService.cs (auto-update status on order events)

namespace CoffeeShopPOS.Models
{
    /// <summary>
    /// Represents a restaurant table with seating capacity and availability status.
    /// Populated by TableDAL.cs from the tables database table.
    /// </summary>
    public class Table
    {
        // Primary key — auto-incremented in MySQL
        public int TableId { get; set; }

        // Unique identifier displayed in the UI (e.g., "T1", "T2", "Patio-1")
        // Shown in TableManagementForm.cs visual grid and OrderForm.cs dropdown
        public string TableNumber { get; set; } = string.Empty;

        // Maximum number of guests this table can seat
        public int Capacity { get; set; }

        // Current status — determines colour coding in TableManagementForm.cs:
        //   "Available" = Green, "Occupied" = Red, "Reserved" = Yellow
        // Updated automatically by OrderService.cs when orders are placed/completed
        public string Status { get; set; } = "Available";

        /// <summary>
        /// Override ToString for display in ComboBox controls.
        /// Used in OrderForm.cs table selection dropdown.
        /// </summary>
        public override string ToString() => $"{TableNumber} (Seats: {Capacity}) — {Status}";
    }
}

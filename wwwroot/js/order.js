$(document).ready(function () {
  let queryString = window.location.search;
  const searchParams = new URLSearchParams(queryString);
  const status = searchParams.get("status");
  loadDataTable(status);
});

let datatable;

function loadDataTable(status) {
  datatable = $("#tblData").DataTable({
    ajax: { url: "/admin/order/getall?status=" + status },
    columns: [
      { data: "id", width: "5%" },
      { data: "name", width: "15%" },
      { data: "phoneNumber", width: "15%" },
      { data: "applicationUser.email", width: "15%" },
      { data: "orderStatus", width: "10%" },
      { data: "orderTotal", width: "10%" },
      {
        data: "id",
        render: function (data) {
          return `
            <div class="w-75 btn-group" role="group">
                <a href="/admin/order/details?orderId=${data}" class="btn btn-primary mx-2">
                    <i class="bi bi-pencil-square"></i>
                    Edit
                </a>
            </div>
        `;
        },
        width: "10%",
      },
    ],
  });
}

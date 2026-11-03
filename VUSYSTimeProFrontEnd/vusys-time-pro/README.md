# vusys-time-pro

Interactive React JSX foundation for the TimePro Timesheet Management application.

## Included

- Vite + React JSX
- Bootstrap 5 and colorful Bootstrap icons
- Responsive desktop/mobile application shell
- Collapsible sidebar and mobile offcanvas navigation
- Functional menu search with keyboard navigation (`Ctrl/Cmd + K`)
- Light/dark theme toggle
- User dropdown: User Profile, Change Password, Logout
- Persistent route tabs with protected routes
- Login, Forgot Password, Change Password and User Profile screens
- Splash screen, offline indicator and PWA configuration
- Interactive home dashboard
- Reusable `InlineEditable` and `SortableTable` components

## Run

```bash
npm install
npm run dev
```

## Demo login

- Email: `admin@timepro.com`
- Password: `Admin@123`

The current authentication provider is a frontend demo and is ready to be replaced with the TimePro APIs.

## Large-data grid mode

`SortableTable` and `InlineEditableTable` support both client-side and server-side processing.
For large datasets such as 100,000 records, set `serverSide` and return only the current page from the API.

```jsx
<SortableTable
  columns={columns}
  data={response.data}
  serverSide
  totalRecords={response.totalRecords}
  defaultPageSize={20}
  pageSizeOptions={[20, 50, 100]}
  loading={loading}
  onQueryChange={loadEmployees}
/>
```

`onQueryChange` receives:

```js
{
  pageNumber: 1,
  pageSize: 20,
  searchText: "Amit",
  sortColumn: "employeeName",
  sortDirection: "asc",
  filters: { department: "Engineering" }
}
```

Expected API response:

```js
{
  data: [],
  totalRecords: 100000,
  pageNumber: 1,
  pageSize: 20,
  totalPages: 5000
}
```

In server-side mode the component does not search, filter, sort, or paginate the full dataset in the browser. It renders only the page returned by the API.

## Environment configuration

TimePro uses Vite mode-specific environment files:

- `.env` for local development and the default build
- `.env.staging` for staging commands
- `.env.prod` for production commands

Use the shared helpers from `src/utils/environmentConfig.js` instead of reading `import.meta.env` throughout individual components.

Available commands:

```bash
npm run dev
npm run dev:staging
npm run build
npm run build:staging
npm run build:prod
npm run preview
npm run preview:staging
npm run lint
```

`VITE_VERISON_NO` is compared with the version stored in the browser after authentication. When the value changes, the authenticated application reloads once and stores the new version.

# VUSYS AI Code Review — TimePro-based React Frontend

This frontend is **derived from the user-provided latest TimePro React source**. It retains the original TimePro `styles.css`, `AppLayout`, `Header`, `Sidebar`, `Footer`, `RouteTabs`, `AuthShell`, `ConfirmModal`, `SessionTimeoutModal`, `ToastService`, PWA configuration, form components, reusable utilities, and errors screens. Business-specific TimePro pages were replaced with GitHub/Gemini pages. Auth, tabs, and API client were adapted to the existing CodeReview.Api contract.

## Run
1. Start the original CodeReview.Api backend at `http://localhost:5098` (set Auth, Gemini, GitHub secrets on backend).
2. `npm install`
3. `npm run dev`
4. Open `http://localhost:5173` and log in using the backend admin username/password.
5. `npm run build` for production validation.

## API endpoints
- `POST /api/auth/login`
- `GET /api/repositories`
- `GET /api/repositories/{owner}/{repo}/pulls`
- `POST /api/reviews/analyze`
- `POST /api/reviews/publish`
- `GET /api/reviews`

## Notes
- Original TimePro CSS and shared components are retained, with separate code-review-specific CSS additions.
- Existing CodeReview.Api has a single administrator account and does not provide TimePro password-reset, profile, menu-access or server-side tab endpoints. Those features are adapted to the available backend instead of making nonfunctional calls.
- Login uses Username rather than Email ID because the existing backend expects `username`.
- GitHub publishing requires explicit confirmation; this frontend never approves or merges PRs.
- Do not commit real API keys, tokens or passwords. The frontend has no secrets.
- The frontend has not been end-to-end tested against a live Gemini API or GitHub repository in this environment.


## UI refinements (2026-11-21)
- Three home cards per row on wide screens; two on tablets, one on phones.
- Scoped TimePro-style table, buttons, pagination and explicit dark theme styles.
- Pull Request repository validation is inline with red border and no validation toast.
- AI Review inputs use inline validation. API/network failures still use existing global toast error handling.
- Existing backend API contracts and application contexts remain unchanged.

## Inspect pull request changes

Start the updated Web API on port 5098. Run `npm ci` and `npm run dev`, open the Vite URL, and sign in. The development server proxies `/api` to the API. For deployments with a separate API origin, set `VITE_API_BASE_URL` to the backend API base URL (including `/api`) and configure the API to allow the frontend origin.

Open **GitHub → Pull Requests**, load an allowed `owner/repository`, and select **View Changes**. The workspace retrieves the PR summary and Files Changed immediately. Expand a file to view its unified diff or select **View before and after** to load the underlying file versions. You can also enter the repository and PR number directly in the Review Workspace.

GitHub can omit patches and may return partial diffs. The page shows that status alongside the file. **Run Gemini AI Review** is a separate action.

## Review Workspace

Open a pull request with **View Changes** and switch between **Changed Files**, **AI Review**, and **Manual Review**. Open **View before and after** for a full-width comparison. Removed lines are marked in red and added lines in green using the available patch. Each pane starts near its first change.

Manual reviews support a general comment and up to 20 inline comments. Select the plus button beside a new-side line in a complete patch to draft an inline comment. The paired API must be running before publishing. Manual reviews post to GitHub as COMMENT reviews and are not stored in the portal's AI review history.

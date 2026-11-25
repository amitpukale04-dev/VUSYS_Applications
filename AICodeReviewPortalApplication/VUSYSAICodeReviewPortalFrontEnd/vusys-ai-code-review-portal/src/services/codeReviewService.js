import apiClient from "./apiClient";
export const listRepositories=async(options)=> (await apiClient.get("/repositories",options)).data;
export const listPullRequests=async(owner,repo,options)=> (await apiClient.get(`/repositories/${encodeURIComponent(owner)}/${encodeURIComponent(repo)}/pulls`,options)).data;
export const listReviews=async()=> (await apiClient.get("/reviews")).data;
export const getReview=async(id)=> (await apiClient.get(`/reviews/${id}`)).data;
export const analyzePullRequest=async(owner,repo,number)=> (await apiClient.post("/reviews/analyze",{owner,repo,number})).data;
export const publishReview=async(reviewId,owner,repo,number)=> (await apiClient.post("/reviews/publish",{reviewId,owner,repo,number})).data;

const pullRequestPath = (owner, repo, number) =>
  `/PullRequests/${encodeURIComponent(owner)}/${encodeURIComponent(repo)}/${number}`;

export const getPullRequestFiles = async (owner, repo, number, signal) =>
  (await apiClient.get(`${pullRequestPath(owner, repo, number)}/Files`, { signal, skipGlobalErrorToast: true })).data;

export const getPullRequestFileContent = async (owner, repo, number, fileName, signal) =>
  (await apiClient.get(`${pullRequestPath(owner, repo, number)}/FileContent`, {
    params: { fileName },
    skipGlobalErrorToast: true,
    signal,
  })).data;

export const publishManualReview = async (review) =>
  (await apiClient.post("/reviews/manual/publish", review, { skipGlobalErrorToast: true })).data;

export const getPullActionStatus = async (owner, repo, number, signal) =>
  (await apiClient.get(`${pullRequestPath(owner, repo, number)}/ActionStatus`, { signal, skipGlobalErrorToast: true })).data;

export const runPullAction = async (owner, repo, number, action, headSha) =>
  (await apiClient.post(`${pullRequestPath(owner, repo, number)}/${action}`, { headSha }, {
    skipGlobalErrorToast: true,
  })).data;

export const getReviewThreads = async (path, cursor, signal) =>
  (await apiClient.get(`${pullRequestPath(...path)}/ReviewThreads`, {
    params: { cursor }, signal, skipGlobalErrorToast: true,
  })).data;

export const getReviewThreadComments = async (path, threadId, cursor) =>
  (await apiClient.get(`${pullRequestPath(...path)}/ReviewThreadComments`, {
    params: { threadId, cursor }, skipGlobalErrorToast: true,
  })).data;

export const updateReviewThread = async (path, threadId, body) =>
  apiClient.post(`${pullRequestPath(...path)}/ReviewThreads/${body === undefined ? "Resolve" : "Reply"}`,
    { threadId, body }, { skipGlobalErrorToast: true });

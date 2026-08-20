---
name: reddit-crawler
description: Use when you need to search Reddit for posts and comments without an API key, fetch post bodies and comment threads, or monitor Reddit for keyword mentions in the last 24-48 hours. Covers the public JSON endpoint trick, URL patterns, response structure, pagination, and how to extract post body plus top comments.
---

# Reddit Crawler (No API Key Required)

## Overview

Reddit exposes every page as JSON by appending `.json` to any URL. No API key, no OAuth, no rate-limit registration. You can search posts, browse subreddits, and fetch full comment threads using only the WebFetch tool.

---

## URL Patterns

### Search all of Reddit

```
https://www.reddit.com/search.json?q=QUERY&sort=new&t=day&limit=25
```

| Parameter | Values | Notes |
|---|---|---|
| `q` | URL-encoded search string | Use `+` for spaces: `freelance+skill+exchange` |
| `sort` | `new`, `hot`, `relevance`, `top` | Use `new` for recency monitoring |
| `t` | `hour`, `day`, `week`, `month`, `year`, `all` | `day` = last 24h |
| `limit` | 1-100 | 25 is safe; 100 is max |
| `after` | token from previous response | For pagination |

### Search within one subreddit

```
https://www.reddit.com/r/SUBREDDIT/search.json?q=QUERY&sort=new&t=day&restrict_sr=1&limit=25
```

### Browse new posts in a subreddit (no query)

```
https://www.reddit.com/r/freelance/new.json?limit=25
```

### Fetch a single post with its comments

```
https://www.reddit.com/r/SUBREDDIT/comments/POST_ID.json?limit=50&depth=3
```

Example: if `permalink` is `/r/freelance/comments/abc123/my_question/`, fetch:

```
https://www.reddit.com/r/freelance/comments/abc123.json?limit=50&depth=3
```

---

## How to Fetch

**WebFetch cannot fetch reddit.com — it is blocked.** Use the Playwright `browser_navigate` tool instead.

```
# Step 1: Navigate with Playwright
browser_navigate("https://www.reddit.com/r/freelance/comments/abc123.json?limit=100&depth=5")
```

The response will almost always exceed the token limit and be saved to a `.txt` file. When that happens, parse it with the reusable script:

```bash
python scripts/reddit_fetch.py          # auto-uses latest saved file
python scripts/reddit_fetch.py <path>   # explicit file path
python scripts/reddit_fetch.py <path> --comment <comment_id>  # highlight one comment
```

Do not use `old.reddit.com` — it blocks more aggressively.

---

## JSON Response Structure

### Search / listing response

```json
{
  "data": {
    "after": "t3_abc123",
    "children": [
      {
        "kind": "t3",
        "data": {
          "id": "abc123",
          "title": "Post title here",
          "selftext": "Full post body text",
          "author": "username",
          "subreddit": "freelance",
          "permalink": "/r/freelance/comments/abc123/post_title/",
          "score": 47,
          "num_comments": 12,
          "created_utc": 1741234567,
          "upvote_ratio": 0.94,
          "is_self": true
        }
      }
    ]
  }
}
```

**Key fields to extract per post:**

| Field | What it is |
|---|---|
| `id` | Post ID (use to fetch comments) |
| `title` | Post title |
| `selftext` | Full post body (empty string if link post) |
| `author` | Username of poster |
| `subreddit` | Subreddit name (no r/ prefix) |
| `permalink` | Relative URL — append to `https://www.reddit.com` |
| `score` | Net upvotes |
| `num_comments` | Total comment count |
| `created_utc` | Unix timestamp — convert to check recency |
| `archived` | `true` = post is locked after 6 months |
| `locked` | `true` = manually locked by mods |

### Post + comments response

Fetching a post with `.json` returns an **array of two objects**:
- Index `[0]` = the post itself
- Index `[1]` = top-level comments

### Comment structure

```json
{
  "kind": "t1",
  "data": {
    "id": "xyz789",
    "author": "commenter_username",
    "body": "Comment text here",
    "score": 23,
    "created_utc": 1741234900,
    "replies": { "data": { "children": [...] } }
  }
}
```

`replies` is recursive. If no replies, it's an empty string `""` instead of an object.

---

## Pagination

```
https://www.reddit.com/search.json?q=freelance+skill+exchange&sort=new&t=day&limit=100&after=t3_abc123
```

Stop paginating when `data.after` is `null`.

---

## Rate Limits

- Unauthenticated: roughly 60 requests per minute
- Space requests at least 1 second apart
- If you get a 429, wait and retry once. Do not loop.

---

## Daily Monitoring Workflow

1. Load `keywords.md` from the `reddit-marketing` skill
2. For each query in the "Quick Daily Scan" section, fetch the search URL
3. For subreddit-targeted queries, use `restrict_sr=1`
4. Filter: skip posts older than 24h, archived, or locked
5. Score each result using the scoring rubric in `keywords.md`
6. For posts scoring 3+, fetch the full comment thread
7. Pass qualifying posts to the `reddit-marketing` skill to draft a sniper comment

---

## Common Issues

| Problem | Fix |
|---|---|
| Response is HTML (login page) | Make sure URL ends in `.json` |
| `selftext` is `"[removed]"` | Post removed by mods. Skip it. |
| `selftext` is `""` | Link post — content is at `url` |
| `archived: true` | Comments locked — skip for sniper use |
| `locked: true` | Manually locked — skip for sniper use |
| 429 Too Many Requests | Wait 60 seconds. Do not retry in a loop. |

---

## Quick Reference: Build a URL

```
# Search all Reddit, last 24h
https://www.reddit.com/search.json?q=skill+exchange+freelance&sort=new&t=day&limit=25

# Scoped to r/freelance
https://www.reddit.com/r/freelance/search.json?q=barter+skills&sort=new&t=day&restrict_sr=1&limit=25

# All new posts in r/Entrepreneur
https://www.reddit.com/r/Entrepreneur/new.json?limit=50

# Fetch post + top 25 comments
https://www.reddit.com/r/startups/comments/abc123.json?limit=25&depth=2
```

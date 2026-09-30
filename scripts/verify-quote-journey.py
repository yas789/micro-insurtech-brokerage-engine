#!/usr/bin/env python3
"""Assert HTTP quote results against their actual SQL Server records."""

import argparse
import json
import os
import subprocess
import sys
from urllib.error import HTTPError
from urllib.request import Request, urlopen
from uuid import uuid4


def check(condition, message):
    if not condition:
        raise AssertionError(message)


def request(base_url, payload=None, path="/api/quotes"):
    data = None if payload is None else json.dumps(payload).encode()
    message = Request(base_url.rstrip("/") + path, data=data, headers={"Content-Type": "application/json"})
    try:
        response = urlopen(message, timeout=30)
    except HTTPError as error:
        response = error
    with response:
        return response.status, json.load(response)


def payload(email, postcode="SW1A 1AA"):
    return {
        "client": {"firstName": "Jane", "lastName": "Broker", "email": email},
        "property": {"postcode": postcode, "yearBuilt": 1910, "rebuildCost": 750000, "isUnoccupied": False},
    }


def persisted_rows(email):
    escaped_email = email.replace("'", "''")
    query = f"""SET NOCOUNT ON;
        SELECT c.ClientID, c.FirstName, c.LastName, c.Email,
               p.PropertyID, p.Postcode, p.YearBuilt,
               CONVERT(varchar(30), p.RebuildCost), CONVERT(int, p.IsUnoccupied),
               ISNULL(p.Region, '<NULL>'), q.QuoteID, q.UnderwriterName,
               CONVERT(varchar(30), q.PremiumAmount), q.RiskRating, ISNULL(q.Region, '<NULL>')
        FROM dbo.Clients c
        LEFT JOIN dbo.Properties p ON p.ClientID = c.ClientID
        LEFT JOIN dbo.Quotes q ON q.PropertyID = p.PropertyID
        WHERE c.Email = '{escaped_email}'
        ORDER BY q.PremiumAmount, q.UnderwriterName;"""
    environment = dict(os.environ, SQLCMDPASSWORD=os.getenv("SQLSERVER_SA_PASSWORD", "Change_this_password_123!"))
    command = [
        "docker", "exec", "-e", "SQLCMDPASSWORD",
        os.getenv("SQLSERVER_CONTAINER_NAME", "micro-insurtech-sql"),
        "/opt/mssql-tools18/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-C", "-b",
        "-d", os.getenv("SQLSERVER_DATABASE_NAME", "MicroInsurTech"),
        "-h", "-1", "-s", "|", "-W", "-w", "65535", "-Q", query,
    ]
    result = subprocess.run(command, env=environment, capture_output=True, text=True, timeout=30)
    if result.returncode != 0:
        raise RuntimeError(f"SQL lookup failed: {result.stderr or result.stdout}")
    rows = []
    for line in result.stdout.splitlines():
        if not line.strip() or line.startswith("("):
            continue
        fields = line.split("|")
        if len(fields) != 15:
            raise RuntimeError(f"Unexpected SQL row shape: {line}")
        rows.append({
            "ClientID": int(fields[0]),
            "FirstName": fields[1],
            "LastName": fields[2],
            "Email": fields[3],
            "PropertyID": int(fields[4]),
            "Postcode": fields[5],
            "YearBuilt": int(fields[6]),
            "RebuildCost": float(fields[7]),
            "IsUnoccupied": fields[8] == "1",
            "PropertyRegion": None if fields[9] == "<NULL>" else fields[9],
            "QuoteID": int(fields[10]),
            "UnderwriterName": fields[11],
            "PremiumAmount": float(fields[12]),
            "RiskRating": fields[13],
            "Region": None if fields[14] == "<NULL>" else fields[14],
        })
    return rows


def verify_quotes(quotes):
    expected = [
        ("NichePropertyCover", 156, "Medium"),
        ("AXAScheme", 180, "Low"),
        ("AvivaScheme", 210, "Medium"),
    ]
    actual = [(quote["underwriterName"], quote["premiumAmount"], quote["riskRating"]) for quote in quotes]
    check(actual == expected, f"Unexpected quote pricing or ordering: {actual}")


def verify_persistence(email, quotes, postcode="SW1A 1AA"):
    rows = persisted_rows(email)
    check(len(rows) == 3, f"Expected three saved quotes for {email}, found {len(rows)}")
    check(len({row["ClientID"] for row in rows}) == 1, "Expected exactly one saved client")
    check(len({row["PropertyID"] for row in rows}) == 1, "Expected exactly one saved property")
    check(len({row["QuoteID"] for row in rows}) == 3, "Expected three distinct saved quote IDs")
    for row, quote in zip(rows, quotes):
        check((row["FirstName"], row["LastName"], row["Email"]) == ("Jane", "Broker", email), "Client data mismatch")
        check((row["Postcode"], row["YearBuilt"], row["RebuildCost"], row["IsUnoccupied"])
              == (postcode, 1910, 750000, False), "Property data mismatch")
        check((row["UnderwriterName"], row["PremiumAmount"], row["RiskRating"], row["Region"])
              == (quote["underwriterName"], quote["premiumAmount"], quote["riskRating"], quote.get("region")),
              "Saved quote differs from HTTP response")
        check(row["PropertyRegion"] == quote.get("region"), "Saved property region differs from quote")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default=os.getenv("FRONTEND_BASE_URL", "http://127.0.0.1:3000"))
    parser.add_argument("--postcode-fixture", action="store_true", help="Require deterministic London and 503 fixture cases")
    parser.add_argument("--unavailable-gateway-url", help="A second gateway configured to point at an unavailable core API")
    parser.add_argument("--verify-saved-response", action="store_true", help="Check browser email, postcode, and quotes JSON from stdin against SQL")
    parser.add_argument("--check-initializer", action="store_true", help="Re-run database initialization and verify existing samples survive")
    args = parser.parse_args()

    if args.verify_saved_response:
        saved = json.load(sys.stdin)
        verify_quotes(saved["quotes"])
        verify_persistence(saved["email"], saved["quotes"], saved["postcode"])
        print("PASS: browser results match persisted SQL records")
        return

    status, health = request(args.base_url, path="/health")
    check(status == 200 and health == {"status": "PHP gateway running"}, "Gateway health failed")
    cases = [("SW1A 1AA", "London")]
    if args.postcode_fixture:
        cases.append(("ZZ99 9ZZ", None))
    saved_samples = []
    for postcode, region in cases:
        email = f"smoke-{uuid4().hex}@example.com"
        status, body = request(args.base_url, payload(email, postcode))
        check(status == 200, f"Quote request failed: {status} {body}")
        quotes = body["quotes"]
        verify_quotes(quotes)
        if args.postcode_fixture:
            check(all(quote.get("region") == region for quote in quotes), "Postcode enrichment/fallback mismatch")
        verify_persistence(email, quotes, postcode)
        saved_samples.append((email, quotes, postcode))
        print(f"PASS: {postcode} returned three ranked quotes and matching SQL records ({email})")

    status, body = request(args.base_url, {})
    check(status == 400 and body.get("errors"), "Gateway should reject missing client/property data")
    email = f"invalid-{uuid4().hex}@example.com"
    invalid = payload(email)
    invalid["property"]["yearBuilt"] = 1499
    status, body = request(args.base_url, invalid)
    check(status == 400 and body.get("errors"), "Core validation should reject year 1499")
    check(persisted_rows(email) == [], "Invalid request must not persist a client")
    print("PASS: gateway and core validation errors propagate without persistence")

    if args.unavailable_gateway_url:
        email = f"unavailable-{uuid4().hex}@example.com"
        status, body = request(args.unavailable_gateway_url, payload(email))
        check(status == 502 and body == {"error": "Core engine is unavailable."}, "Expected safe core-unavailable response")
        check(persisted_rows(email) == [], "Unavailable core must not persist a client")
        print("PASS: core unavailability produces a safe gateway error")

    if args.check_initializer:
        from pathlib import Path
        subprocess.run(["sh", str(Path(__file__).resolve().with_name("apply-local-schema.sh"))], check=True, timeout=30)
        for email, quotes, postcode in saved_samples:
            verify_persistence(email, quotes, postcode)
        print("PASS: repeated schema initialization preserves saved records")


if __name__ == "__main__":
    try:
        main()
    except (AssertionError, OSError, ValueError, KeyError, subprocess.SubprocessError) as error:
        print(f"Quote journey verification failed: {error}", file=sys.stderr)
        sys.exit(1)

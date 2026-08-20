#!/bin/bash
# SkillLedger Test Execution Script (Bash)
# Supports selective test execution with parallel optimization

set -euo pipefail

# Default values
CATEGORY="All"
COVERAGE=false
VERBOSE=false
WATCH=false
THREADS=4

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Help function
show_help() {
    echo -e "${CYAN}🧪 SkillLedger Test Runner${NC}"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -c, --category CATEGORY    Test category (Fast, Unit, Integration, Security, Performance, EndToEnd, BDD, Financial, Core, Messaging, Document, All)"
    echo "  --backend                  Run all backend (.NET) tests"
    echo "  --frontend                 Run all frontend (Next.js) tests"
    echo "  --coverage                 Enable code coverage collection"
    echo "  -v, --verbose              Verbose test output"
    echo "  -w, --watch                Run in watch mode"
    echo "  -t, --threads NUM          Number of parallel threads (default: 4)"
    echo "  -h, --help                 Show this help message"
    echo ""
    echo -e "${CYAN}💡 Quick Commands:${NC}"
    echo -e "   ${GREEN}./scripts/run-tests.sh --backend${NC}     - Run all backend tests"
    echo -e "   ${GREEN}./scripts/run-tests.sh --frontend${NC}    - Run all frontend tests"
    echo ""
    echo -e "${CYAN}💡 Available test categories:${NC}"
    echo -e "   ${GRAY}- Fast: Quick unit tests (< 100ms)${NC}"
    echo -e "   ${GRAY}- Unit: All unit tests${NC}"
    echo -e "   ${GRAY}- Integration: Database and API tests${NC}"
    echo -e "   ${GRAY}- Security: Security-focused tests${NC}"
    echo -e "   ${GRAY}- Performance: Performance benchmarks${NC}"
    echo -e "   ${GRAY}- EndToEnd: Full workflow tests${NC}"
    echo -e "   ${GRAY}- BDD: Behavior-driven development tests${NC}"
    echo -e "   ${GRAY}- Financial: Financial domain tests${NC}"
    echo -e "   ${GRAY}- Core: Core business logic tests${NC}"
    echo -e "   ${GRAY}- Messaging: Real-time messaging tests${NC}"
    echo -e "   ${GRAY}- Document: Document management tests${NC}"
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--category)
            CATEGORY="$2"
            shift 2
            ;;
        --coverage)
            COVERAGE=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -w|--watch)
            WATCH=true
            shift
            ;;
        -t|--threads)
            THREADS="$2"
            shift 2
            ;;
        --backend)
            CATEGORY="Backend"
            shift
            ;;
        --frontend)
            CATEGORY="Frontend" 
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            echo -e "${RED}❌ Unknown option: $1${NC}"
            show_help
            exit 1
            ;;
    esac
done

# Validate category
case $CATEGORY in
    Fast|Unit|Integration|Security|Performance|EndToEnd|BDD|Financial|Core|Messaging|Document|All|Backend|Frontend)
        ;;
    *)
        echo -e "${RED}❌ Invalid category: $CATEGORY${NC}"
        show_help
        exit 1
        ;;
esac

# Get project root directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$PROJECT_ROOT"

echo -e "${CYAN}🧪 SkillLedger Test Runner${NC}"
echo -e "${GRAY}📂 Project: $PROJECT_ROOT${NC}"
echo -e "${YELLOW}🔧 Category: $CATEGORY${NC}"

# Handle Backend and Frontend separately
if [[ "$CATEGORY" == "Backend" ]]; then
    echo -e "${CYAN}🔧 Running Backend (.NET) Tests${NC}"
    
    # Build dotnet test command for backend
    TEST_ARGS=(
        "test"
        "tests/SkillLedger.Tests/"
        "--nologo"
        "--configuration" "Debug"
        "--settings" "tests/runsettings.xml"
    )
    
    FILTER=""
    
elif [[ "$CATEGORY" == "Frontend" ]]; then
    echo -e "${CYAN}🔧 Running Frontend (Next.js) Tests${NC}"
    cd "$PROJECT_ROOT/web"
    
    # Build npm/yarn test command for frontend
    if command -v yarn &> /dev/null; then
        TEST_COMMAND="yarn"
    else
        TEST_COMMAND="npm run"
    fi
    
    if [[ "$COVERAGE" == "true" ]]; then
        TEST_SCRIPT="test:coverage"
    elif [[ "$WATCH" == "true" ]]; then
        TEST_SCRIPT="test:watch"
    else
        TEST_SCRIPT="test"
    fi
    
    echo -e "${GREEN}🎯 Using: $TEST_COMMAND $TEST_SCRIPT${NC}"
    
else
    # Build test filter based on category for .NET tests
    case $CATEGORY in
        Fast)        FILTER="FullyQualifiedName~FastTest" ;;
        Unit)        FILTER="FullyQualifiedName~Unit" ;;
        Integration) FILTER="FullyQualifiedName~Integration" ;;
        Security)    FILTER="FullyQualifiedName~Security" ;;
        Performance) FILTER="FullyQualifiedName~Performance" ;;
        EndToEnd)    FILTER="FullyQualifiedName~EndToEnd" ;;
        BDD)         FILTER="FullyQualifiedName~BDD" ;;
        Financial)   FILTER="FullyQualifiedName~Financial" ;;
        Core)        FILTER="FullyQualifiedName~Core" ;;
        Messaging)   FILTER="FullyQualifiedName~Messaging" ;;
        Document)    FILTER="FullyQualifiedName~Document" ;;
        All)         FILTER="" ;;
    esac

    # Build dotnet test command
    TEST_ARGS=(
        "test"
        "tests/SkillLedger.Tests/"
        "--nologo"
        "--configuration" "Debug"
        "--settings" "tests/runsettings.xml"
    )
fi

if [[ "$CATEGORY" != "Frontend" && -n "$FILTER" ]]; then
    TEST_ARGS+=("--filter" "$FILTER")
    echo -e "${GREEN}🎯 Filter: $FILTER${NC}"
fi

if [[ "$COVERAGE" == "true" ]]; then
    TEST_ARGS+=("--collect:XPlat Code Coverage")
    echo -e "${GREEN}📊 Coverage: Enabled${NC}"
else
    echo -e "${GRAY}📊 Coverage: Disabled (use --coverage to enable)${NC}"
fi

if [[ "$VERBOSE" == "true" ]]; then
    TEST_ARGS+=("--verbosity" "detailed")
    echo -e "${GREEN}📝 Verbosity: Detailed${NC}"
else
    TEST_ARGS+=("--verbosity" "minimal")
fi

if [[ "$WATCH" == "true" ]]; then
    TEST_ARGS+=("--watch")
    echo -e "${GREEN}👀 Watch Mode: Enabled${NC}"
fi

# Set parallel execution environment
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export MSBUILDDISABLENODEREUSE=1

echo -e "${CYAN}🚀 Starting tests...${NC}"
echo -e "${YELLOW}⚡ Parallel Threads: $THREADS${NC}"

# Execute tests
START_TIME=$(date +%s)

if [[ "$CATEGORY" == "Frontend" ]]; then
    # Execute frontend tests
    if $TEST_COMMAND $TEST_SCRIPT; then
        END_TIME=$(date +%s)
        DURATION=$((END_TIME - START_TIME))
        echo -e "${GREEN}✅ Frontend tests completed successfully!${NC}"
        echo -e "${GREEN}⏱️  Duration: ${DURATION}s${NC}"
    else
        EXIT_CODE=$?
        echo -e "${RED}❌ Frontend tests failed with exit code: $EXIT_CODE${NC}"
        exit $EXIT_CODE
    fi
else
    # Execute backend tests
    if dotnet "${TEST_ARGS[@]}"; then
        END_TIME=$(date +%s)
        DURATION=$((END_TIME - START_TIME))
        echo -e "${GREEN}✅ Backend tests completed successfully!${NC}"
        echo -e "${GREEN}⏱️  Duration: ${DURATION}s${NC}"
    else
        EXIT_CODE=$?
        echo -e "${RED}❌ Backend tests failed with exit code: $EXIT_CODE${NC}"
        exit $EXIT_CODE
    fi
fi

echo ""
echo -e "${CYAN}💡 Usage examples:${NC}"
echo -e "${GRAY}  ./scripts/run-tests.sh --backend${NC}"
echo -e "${GRAY}  ./scripts/run-tests.sh --frontend${NC}"
echo -e "${GRAY}  ./scripts/run-tests.sh --backend --coverage${NC}"
echo -e "${GRAY}  ./scripts/run-tests.sh --frontend --watch${NC}"
echo -e "${GRAY}  ./scripts/run-tests.sh --category Unit${NC}"
echo -e "${GRAY}  ./scripts/run-tests.sh --category Integration --verbose${NC}"
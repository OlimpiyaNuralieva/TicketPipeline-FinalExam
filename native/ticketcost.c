#include "ticketcost.h"

static const int32_t RATE_CENTS[3] = { 2050, 3575, 6025 };

#define DISCOUNT_FROM_EFFORT 6
#define DISCOUNT_PERCENT     5

int32_t ticket_cost_cents(int32_t kind, int32_t effort)
{
    if (kind < 0 || kind > 2)
        return TICKETCOST_ERR_KIND;
    if (effort < 1 || effort > 8)
        return TICKETCOST_ERR_EFFORT;

    int32_t base = RATE_CENTS[kind] * effort;

    if (effort >= DISCOUNT_FROM_EFFORT) {
        int32_t discount = (base * DISCOUNT_PERCENT + 50) / 100;
        return base - discount;
    }
    return base;
}
